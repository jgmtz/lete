using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Lete.Game.UI;

/// <summary>Uma opção de <see cref="ChoiceScreen"/>: o texto, o que faz, se está disponível e se aparece esmaecida.</summary>
public sealed record Choice(string Text, Action Act, bool Enabled = true, bool Dim = false);

/// <summary>
/// Base das telas provisórias de escolha (loja, Trívia, julgamento), até haver arte: um título, um texto e uma lista
/// de opções; <c>cancel</c> fecha. As subclasses montam o conteúdo.
/// </summary>
public abstract partial class ChoiceScreen : CanvasLayer
{
    [Export] public Vector2 PanelSize { get; set; } = new(1200, 560);

    private PanelContainer _panel = null!;
    private Label _title = null!;
    private Label _text = null!;
    private VBoxContainer _buttons = null!;

    public bool IsOpen => _panel.Visible;

    public event Action? OpenChanged;

    public override void _Ready()
    {
        Layer = 7;
        _panel = UiStyle.MakePanel();
        _panel.CustomMinimumSize = PanelSize;
        AddChild(_panel);
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center, Control.LayoutPresetMode.KeepSize);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 16);
        _panel.AddChild(column);
        _title = UiStyle.MakeLabel("", UiStyle.TitleFontSize);
        column.AddChild(_title);
        _text = UiStyle.MakeLabel("");
        _text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(_text);
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        column.AddChild(scroll);
        _buttons = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _buttons.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_buttons);

        _panel.Visible = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (IsOpen && @event.IsActionPressed("cancel"))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>Aperta a opção disponível de número <paramref name="index"/> (conferência pela linha de comando).</summary>
    public void Press(int index) => _buttons.GetChildren().OfType<Button>().Where(b => !b.Disabled).ElementAt(index)
        .EmitSignal(BaseButton.SignalName.Pressed);

    protected void ShowPanel(string title)
    {
        _title.Text = title;
        if (!_panel.Visible)
        {
            _panel.Visible = true;
            OpenChanged?.Invoke();
        }
    }

    protected void ShowChoices(string text, IEnumerable<Choice> choices)
    {
        _text.Text = text;
        foreach (var child in _buttons.GetChildren())
        {
            _buttons.RemoveChild(child);
            child.QueueFree();
        }

        foreach (var choice in choices)
        {
            var button = new Button { Text = choice.Text, Alignment = HorizontalAlignment.Left, Disabled = !choice.Enabled };
            button.Pressed += choice.Act;
            button.AddThemeColorOverride("font_color", choice.Dim ? UiStyle.Palette.IvoryDim : UiStyle.Palette.Ivory);
            button.AddThemeColorOverride("font_disabled_color", UiStyle.Palette.ClayGray);
            _buttons.AddChild(button);
        }

        var first = _buttons.GetChildren().OfType<Button>().FirstOrDefault(b => !b.Disabled);
        Callable.From(() =>
        {
            if (first is not null && IsInstanceValid(first) && first.IsInsideTree())
            {
                first.GrabFocus();
            }
        }).CallDeferred();
    }

    protected void Close()
    {
        if (_panel.Visible)
        {
            _panel.Visible = false;
            OpenChanged?.Invoke();
        }
    }

    protected static string T(string key) => GameRoot.Instance.Text(key);
}
