using System;
using System.Linq;
using Godot;
using Lete.Core.Combat.Gestures;

namespace Lete.Game.Combat.Gestures;

/// <summary>
/// Gesto Esquiva (defesa contra golpes de padrão, GDD 4.4): um quadro no centro da tela onde o jogador move
/// um marcador com as ações de movimento, fugindo dos golpes que seguem o gesto da sombra. A regra (quadro,
/// padrão, acertos) fica em <see cref="DodgeRun"/>; aqui só entrada e desenho, em pixels da arte.
/// </summary>
public partial class DodgeGesture : Control, IGesture
{
    [Export] public BattleHudStyle Style { get; set; } = null!;

    /// <summary>Opacidade do fundo do quadro (a cena continua visível atrás).</summary>
    [Export] public float BoxAlpha { get; set; } = 0.8f;

    /// <summary>Opacidade da abertura dourada da saída (Fugir).</summary>
    [Export] public float ExitAlpha { get; set; } = 0.55f;

    /// <summary>Brilho em volta do ponto do jogador: largura em pixels da arte e opacidade.</summary>
    [Export] public float GlowPixels { get; set; } = 1f;
    [Export] public float GlowAlpha { get; set; } = 0.45f;

    /// <summary>Piscadas por segundo do marcador enquanto está protegido depois de um golpe.</summary>
    [Export] public float BlinkPerSecond { get; set; } = 12f;

    /// <summary>Centro do quadro em coordenadas do controle; sem valor, usa o meio do controle.</summary>
    public Vector2? Center { get; set; }

    /// <summary>Quadro e padrão; precisam ser definidos antes de <see cref="Begin"/>.</summary>
    public DodgeSettings Settings { get; set; } = null!;
    public IDodgePattern Pattern { get; set; } = null!;

    /// <summary>Saída do quadro (Fugir); nula numa Esquiva comum.</summary>
    public EscapeExit? Exit { get; set; }

    /// <summary>Onde está o marcador e onde fica a saída, em coordenadas do controle (para o modo demo).</summary>
    public Vector2 MarkerPosition => _run is null ? Vector2.Zero : BoxOrigin + new Vector2(_run.Marker.X, _run.Marker.Y);

    public Vector2? ExitCenter => Exit is { } e
        ? BoxOrigin + new Vector2((e.X + e.Width / 2f) * Settings.BoxWidth, (e.Y + e.Height / 2f) * Settings.BoxHeight)
        : null;

    private Vector2 BoxOrigin => ((Center ?? Size / 2f) - new Vector2(Settings.BoxWidth, Settings.BoxHeight) / 2f).Round();

    private DodgeRun? _run;

    public event Action<float>? Completed;

    /// <summary>O marcador levou um golpe (para quem chama tremer o personagem, por exemplo).</summary>
    public event Action? Hit;

    public bool IsRunning { get; private set; }

    public void Begin()
    {
        _run = new DodgeRun(Settings, Pattern, Exit);
        IsRunning = true;
        Visible = true;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!IsRunning || _run is null)
        {
            return;
        }

        var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        if (_run.Step((float)delta, new System.Numerics.Vector2(input.X, input.Y)))
        {
            Hit?.Invoke();
        }

        if (_run.IsFinished)
        {
            IsRunning = false;
            Completed?.Invoke(_run.Score);
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_run is null)
        {
            return;
        }

        var size = new Vector2(Settings.BoxWidth, Settings.BoxHeight);
        var origin = ((Center ?? Size / 2f) - size / 2f).Round();
        var box = new Rect2(origin, size);

        GestureFrame.Draw(this, box, BoxAlpha);
        if (Exit is { } exit)
        {
            // A saída: uma abertura dourada na borda do quadro.
            var gap = new Rect2(origin + new Vector2(exit.X * size.X, exit.Y * size.Y), new Vector2(exit.Width * size.X, exit.Height * size.Y));
            DrawRect(gap.Grow(1f), new Color(Style.Palette.Gold, ExitAlpha));
        }
        // Golpes recortados no quadro: só aparecem dentro dele.
        foreach (var bullet in _run.Bullets.Select(b => origin + new Vector2(b.X, b.Y)))
        {
            if (box.Grow(-Settings.BulletRadius / 2f).HasPoint(bullet))
            {
                DrawCircle(bullet.Round(), Settings.BulletRadius, Style.Palette.ShadeFill, filled: true, antialiased: false);
            }
        }

        var blinkOff = _run.IsInvulnerable && Mathf.PosMod(_run.Elapsed * BlinkPerSecond, 1f) < 0.5f;
        if (!blinkOff)
        {
            // O ponto do jogador em marfim, com um brilho dourado em volta.
            var marker = (origin + new Vector2(_run.Marker.X, _run.Marker.Y)).Round();
            DrawCircle(marker, Settings.MarkerRadius + GlowPixels, new Color(Style.Palette.Gold, GlowAlpha), filled: true, antialiased: false);
            DrawCircle(marker, Settings.MarkerRadius, Style.Palette.Ivory, filled: true, antialiased: false);
        }
    }
}
