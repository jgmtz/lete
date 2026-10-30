using System;
using Godot;
using Lete.Core.Recollections;
using Lete.Core.State;

namespace Lete.Game.UI;

/// <summary>
/// Cena provisória de uma Memória Pesada (GDD 2.2), até existir o sistema de diálogo: o aviso (Seguir ou "Agora
/// não"), o aflorar (Encarar ou Deixar, definitivo) e o que fica (a verdade, ou a fala de quem viu o jogador deixar).
/// "Agora não" e <c>cancel</c> só valem no aviso; depois dele a escolha é obrigatória.
/// </summary>
public partial class HeavyMemoryScreen : CanvasLayer
{
    [Export] public Vector2 PanelSize { get; set; } = new(1100, 420);

    private PanelContainer _panel = null!;
    private Label _title = null!;
    private Label _text = null!;
    private HBoxContainer _buttons = null!;
    private Recollection? _memory;
    private bool _canPostpone;

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
        column.AddThemeConstantOverride("separation", 24);
        _panel.AddChild(column);
        _title = UiStyle.MakeLabel("", UiStyle.TitleFontSize);
        column.AddChild(_title);
        _text = UiStyle.MakeLabel("");
        _text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _text.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        column.AddChild(_text);
        _buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
        _buttons.AddThemeConstantOverride("separation", 32);
        column.AddChild(_buttons);

        _panel.Visible = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (IsOpen && _canPostpone && @event.IsActionPressed("cancel"))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>Começa pelo aviso da Pesada <paramref name="memoryId"/>.</summary>
    public void Open(string memoryId)
    {
        _memory = GameRoot.Instance.Mosaic.Definitions[memoryId];
        _title.Text = T(_memory.TitleKey);
        Step(_memory.Heavy!.WarningKey, canPostpone: true,
            ("ui.pesada.seguir", ShowSurfacing),
            ("ui.pesada.agora_nao", Close));
        _panel.Visible = true;
        OpenChanged?.Invoke();
    }

    /// <summary>Aperta a opção <paramref name="index"/> da etapa atual (conferência pela linha de comando).</summary>
    public void Press(int index) => _buttons.GetChild<Button>(index).EmitSignal(BaseButton.SignalName.Pressed);

    private void ShowSurfacing() => Step(_memory!.Heavy!.SurfaceKey, canPostpone: false,
        ("ui.pesada.encarar", () => Choose(HeavyMemoryChoice.Face)),
        ("ui.pesada.deixar", () => Choose(HeavyMemoryChoice.Leave)));

    private void Choose(HeavyMemoryChoice choice)
    {
        GameRoot.Instance.HeavyMemories.Choose(_memory!.Id, choice);
        var key = choice == HeavyMemoryChoice.Face ? _memory.StageTextKeys[RecollectionStage.Full] : _memory.Heavy!.LeaveKey;
        Step(key, canPostpone: false, ("ui.pesada.continuar", Close));
    }

    private void Close()
    {
        _panel.Visible = false;
        _memory = null;
        OpenChanged?.Invoke();
    }

    private void Step(string textKey, bool canPostpone, params (string LabelKey, Action Action)[] options)
    {
        _canPostpone = canPostpone;
        _text.Text = T(textKey);
        foreach (var child in _buttons.GetChildren())
        {
            _buttons.RemoveChild(child);
            child.QueueFree();
        }

        Button? first = null;
        foreach (var (labelKey, action) in options)
        {
            var button = new Button { Text = T(labelKey) };
            button.Pressed += action;
            _buttons.AddChild(button);
            first ??= button;
        }

        // Adiado para o botão já estar na árvore; se a etapa passou no mesmo quadro, ele já saiu e nada acontece.
        Callable.From(() =>
        {
            if (first is not null && IsInstanceValid(first) && first.IsInsideTree())
            {
                first.GrabFocus();
            }
        }).CallDeferred();
    }

    private static string T(string key) => GameRoot.Instance.Text(key);
}
