using System;
using System.Linq;
using Godot;
using Lete.Core.State;
using Lete.Game.UI;

namespace Lete.Game.Debug;

/// <summary>
/// Painel de debug (ação "debug_toggle", F1). No M0: render, tamanho do sprite, gênero,
/// teleporte, flags e Diário. Marca, Maré, Lembranças e juramentos entram com os sistemas (M2).
/// </summary>
public partial class DebugPanel : CanvasLayer
{
    private readonly Main _main;
    private PanelContainer _panel = null!;
    private Button _renderButton = null!;
    private Button _genderButton = null!;
    private VBoxContainer _flagList = null!;
    private LineEdit _flagInput = null!;

    public DebugPanel(Main main) => _main = main;

    // Construtor sem parâmetros exigido pelo Godot para tipos de script.
    public DebugPanel() => _main = null!;

    public bool IsOpen => _panel.Visible;

    public event Action? OpenChanged;

    private static string T(string key) => GameRoot.Instance.Text(key);

    public override void _Ready()
    {
        Layer = 10;

        _panel = UiStyle.MakePanel(0.94f);
        _panel.Position = new Vector2(24, 24);
        _panel.CustomMinimumSize = new Vector2(520, 0);
        AddChild(_panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 10);
        _panel.AddChild(column);

        column.AddChild(UiStyle.MakeLabel(T("debug.titulo"), UiStyle.TitleFontSize));

        column.AddChild(UiStyle.MakeLabel(T("debug.render")));
        _renderButton = new Button();
        _renderButton.Pressed += () => _main.SetLowResRender(!_main.LowResRender);
        column.AddChild(_renderButton);

        column.AddChild(UiStyle.MakeLabel(T("debug.sprite")));
        var sizes = new HBoxContainer();
        foreach (var px in Main.SpriteSizes)
        {
            var button = new Button { Text = $"{px} px", CustomMinimumSize = new Vector2(110, 0) };
            button.Pressed += () => _main.SetSpriteSize(px);
            sizes.AddChild(button);
        }
        column.AddChild(sizes);

        column.AddChild(UiStyle.MakeLabel(T("debug.genero")));
        _genderButton = new Button();
        _genderButton.Pressed += ToggleGender;
        column.AddChild(_genderButton);

        column.AddChild(UiStyle.MakeLabel(T("debug.teleporte")));
        var teleports = new HFlowContainer();
        foreach (var point in _main.TeleportPoints)
        {
            var button = new Button { Text = point.Name };
            button.Pressed += () => _main.TeleportPlayer(point);
            teleports.AddChild(button);
        }
        column.AddChild(teleports);

        column.AddChild(UiStyle.MakeLabel(T("debug.flags")));
        _flagList = new VBoxContainer();
        column.AddChild(_flagList);
        var addRow = new HBoxContainer();
        _flagInput = new LineEdit { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _flagInput.TextSubmitted += _ => AddFlag();
        var addButton = new Button { Text = T("debug.flags.adicionar") };
        addButton.Pressed += AddFlag;
        addRow.AddChild(_flagInput);
        addRow.AddChild(addButton);
        column.AddChild(addRow);

        var clearJournal = new Button { Text = T("debug.diario.limpar") };
        clearJournal.Pressed += () => GameRoot.Instance.State.JournalEntries.Clear();
        column.AddChild(clearJournal);

        _panel.Visible = false;
        Refresh();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("debug_toggle"))
        {
            SetOpen(!IsOpen);
            GetViewport().SetInputAsHandled();
        }
    }

    public void SetOpen(bool open)
    {
        _panel.Visible = open;
        Refresh();
        OpenChanged?.Invoke();
    }

    public void Refresh()
    {
        _renderButton.Text = T(_main.LowResRender ? "debug.render.baixa" : "debug.render.direto");
        _genderButton.Text = T(GameRoot.Instance.State.Gender == Gender.Feminine ? "debug.genero.f" : "debug.genero.m");
        RefreshFlags();
    }

    private void ToggleGender()
    {
        var state = GameRoot.Instance.State;
        state.Gender = state.Gender == Gender.Feminine ? Gender.Masculine : Gender.Feminine;
        _main.OnTextsChanged();
        Refresh();
    }

    private void AddFlag()
    {
        var flag = _flagInput.Text.Trim();
        if (flag.Length > 0)
        {
            GameRoot.Instance.State.Flags.Add(flag);
            _flagInput.Clear();
            RefreshFlags();
        }
    }

    private void RefreshFlags()
    {
        foreach (var child in _flagList.GetChildren())
        {
            child.QueueFree();
        }

        var flags = GameRoot.Instance.State.Flags.OrderBy(f => f, StringComparer.Ordinal).ToList();
        if (flags.Count == 0)
        {
            _flagList.AddChild(UiStyle.MakeLabel(T("debug.flags.nenhuma")));
            return;
        }

        foreach (var flag in flags)
        {
            var box = new CheckBox { Text = flag, ButtonPressed = true };
            box.Toggled += on =>
            {
                if (!on)
                {
                    GameRoot.Instance.State.Flags.Remove(flag);
                    CallDeferred(MethodName.RefreshFlags);
                }
            };
            _flagList.AddChild(box);
        }
    }
}
