using System;
using System.Linq;
using Godot;
using Lete.Core.Combat.Gestures;

namespace Lete.Game.Combat.Gestures;

/// <summary>
/// Gesto Arrastar (Oferenda, Libertar; GDD 4.4): conduzir uma luz devagar por um caminho com as ações de
/// movimento. A luz anda presa no caminho (as curvas são automáticas); uma luz-guia vai à frente, devagar, e a
/// luz do jogador esmaece quando se afasta dela (com pressa ou ficando para trás).
/// A regra fica em <see cref="DragRun"/>; aqui só entrada e desenho, em pixels da arte.
/// </summary>
public partial class DragGesture : Control, IGesture
{
    [Export] public BattleHudStyle Style { get; set; } = null!;

    /// <summary>Opacidade do fundo do quadro (a cena continua visível atrás).</summary>
    [Export] public float BoxAlpha { get; set; } = 0.8f;

    /// <summary>Raio da luz e da marca do fim do caminho, em pixels da arte.</summary>
    [Export] public float MarkerRadius { get; set; } = 2f;

    /// <summary>Opacidade mínima da luz quando está bem longe da guia; opacidade da guia.</summary>
    [Export] public float FarMarkerAlpha { get; set; } = 0.35f;
    [Export] public float GuideAlpha { get; set; } = 0.55f;

    /// <summary>Centro do quadro em coordenadas do controle; sem valor, usa o meio do controle.</summary>
    public Vector2? Center { get; set; }

    /// <summary>Quadro e caminho; precisam ser definidos antes de <see cref="Begin"/>.</summary>
    public DragSettings Settings { get; set; } = null!;

    private DragRun? _run;

    public event Action<float>? Completed;

    public bool IsRunning { get; private set; }

    /// <summary>Onde está a luz, para onde o trecho segue e se ela está à frente da guia (para o modo demo).</summary>
    public Vector2 MarkerPosition => _run is null ? Vector2.Zero : Origin + new Vector2(_run.Marker.X, _run.Marker.Y);

    public Vector2 Tangent => _run is null ? Vector2.Right : new Vector2(_run.Tangent.X, _run.Tangent.Y);

    public float Gap => _run?.Gap ?? 0f;

    public Vector2[] PathPoints => Settings.PathPixels.Select(p => Origin + new Vector2(p.X, p.Y)).ToArray();

    private Vector2 Origin => ((Center ?? Size / 2f) - new Vector2(Settings.BoxWidth, Settings.BoxHeight) / 2f).Round();

    public void Begin()
    {
        _run = new DragRun(Settings);
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
        _run.Step((float)delta, new System.Numerics.Vector2(input.X, input.Y));
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

        var box = new Rect2(Origin, new Vector2(Settings.BoxWidth, Settings.BoxHeight));
        GestureFrame.Draw(this, box, BoxAlpha);

        var path = PathPoints;
        DrawPolyline(path.Select(p => p.Round()).ToArray(), new Color(Style.Palette.Gold, 0.6f), 1f, antialiased: false);
        DrawCircle(path[^1].Round(), MarkerRadius + 1f, Style.Palette.Gold, filled: false, antialiased: false);

        // A guia: um ponto dourado mais fraco que anda devagar à frente.
        var guide = (Origin + new Vector2(_run.Guide.X, _run.Guide.Y)).Round();
        DrawCircle(guide, MarkerRadius, new Color(Style.Palette.Gold, GuideAlpha), filled: true, antialiased: false);

        // A luz fica mais fraca quanto mais longe da guia, para o jogador sentir o ritmo sem número.
        var closeness = 1f - Mathf.Clamp(Mathf.Abs(_run.Gap) / (Settings.GapToZero * 2f), 0f, 1f);
        var light = new Color(Style.Palette.Ivory, Mathf.Lerp(FarMarkerAlpha, 1f, closeness));
        DrawCircle(MarkerPosition.Round(), MarkerRadius, light, filled: true, antialiased: false);
    }
}
