using Godot;
using Lete.Game.World;

namespace Lete.Game.UI;

/// <summary>
/// HUD de exploração: aviso de "Examinar" e a frase do objeto examinado. O Diário fica no menu do jogo
/// (<see cref="GameMenu"/>); com o menu aberto, o HUD se esconde.
/// </summary>
public partial class Hud : CanvasLayer
{
    private const double MessageSeconds = 5.0;

    private readonly Player _player;
    private Label _prompt = null!;
    private Label _message = null!;
    private double _messageTimeLeft;

    public Hud(Player player) => _player = player;

    // Construtor sem parâmetros exigido pelo Godot para tipos de script.
    public Hud() => _player = null!;

    private bool _suppressed;

    public override void _Ready()
    {
        Layer = 5;

        _prompt = UiStyle.MakeLabel(GameRoot.Instance.Text("ui.prompt.examinar"));
        AddBottomBand(_prompt, 60, 40);

        _message = UiStyle.MakeLabel("");
        _message.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        AddBottomBand(_message, 120, 90);

        _player.NearbyChanged += RefreshPrompt;
        Interactable.Examined += ShowExamined;
        RefreshPrompt();
        _message.Visible = false;
    }

    public override void _ExitTree()
    {
        Interactable.Examined -= ShowExamined;
    }

    public override void _Process(double delta)
    {
        if (_messageTimeLeft > 0)
        {
            _messageTimeLeft -= delta;
            _message.Visible = _messageTimeLeft > 0;
        }
    }

    /// <summary>Esconde o aviso e a frase enquanto um menu cobre a tela.</summary>
    public void SetSuppressed(bool suppressed)
    {
        _suppressed = suppressed;
        if (suppressed)
        {
            _messageTimeLeft = 0;
            _message.Visible = false;
        }

        RefreshPrompt();
    }

    private void ShowExamined(string key)
    {
        _message.Text = GameRoot.Instance.Text(key);
        _message.Visible = true;
        _messageTimeLeft = MessageSeconds;
    }

    private void RefreshPrompt()
    {
        _prompt.Visible = !_suppressed && _player.CurrentInteractable is not null;
        if (_player.CurrentInteractable is { } target)
        {
            _prompt.Text = GameRoot.Instance.Text(target.PromptKey);
        }
    }

    /// <summary>Faixa horizontal presa embaixo da tela, com o texto centralizado.</summary>
    private void AddBottomBand(Label label, float bottomMargin, float height)
    {
        const float sideMargin = 360;

        AddChild(label);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Bottom;
        label.AnchorLeft = 0;
        label.AnchorRight = 1;
        label.AnchorTop = 1;
        label.AnchorBottom = 1;
        label.OffsetLeft = sideMargin;
        label.OffsetRight = -sideMargin;
        label.OffsetTop = -bottomMargin - height;
        label.OffsetBottom = -bottomMargin;
    }
}
