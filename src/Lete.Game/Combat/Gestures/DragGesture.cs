using System;
using System.Linq;
using Godot;
using Lete.Core.Combat.Gestures;

namespace Lete.Game.Combat.Gestures;

/// <summary>
/// Gesto Arrastar (Oferenda, Libertar; GDD 4.4): conduzir uma luz devagar por um caminho com as ações de
/// movimento. O caminho fica em ouro apagado, o fim em ouro vivo; a luz esmaece quando se afasta do caminho.
/// A regra fica em <see cref="DragRun"/>; aqui só entrada e desenho, em pixels da arte.
/// </summary>
public partial class DragGesture : Control, IGesture
{
    [Export] public BattleHudStyle Style { get; set; } = null!;

    /// <summary>Opacidade do fundo do quadro (a cena continua visível atrás).</summary>
    [Export] public float BoxAlpha { get; set; } = 0.8f;

    /// <summary>Raio da luz e da marca do fim do caminho, em pixels da arte.</summary>
    [Export] public float MarkerRadius { get; set; } = 2f;

    /// <summary>Opacidade mínima da luz quando está bem fora do caminho.</summary>
    [Export] public float FarMarkerAlpha { get; set; } = 0.35f;

    /// <summary>Centro do quadro em coordenadas do controle; sem valor, usa o meio do controle.</summary>
    public Vector2? Center { get; set; }

    /// <summary>Quadro e caminho; precisam ser definidos antes de <see cref="Begin"/>.</summary>
    public DragSettings Settings { get; set; } = null!;

    private DragRun? _run;

    public event Action<float>? Completed;

    public bool IsRunning { get; private set; }

    /// <summary>Onde está a luz e para onde o caminho segue, em coordenadas do controle (para o modo demo).</summary>
    public Vector2 MarkerPosition => _run is null ? Vector2.Zero : Origin + new Vector2(_run.Marker.X, _run.Marker.Y);

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

        // A luz fica mais fraca quanto mais longe do caminho, para o jogador sentir o desvio sem número.
        var closeness = 1f - Mathf.Clamp(_run.Deviation / (Settings.DeviationToZero * 2f), 0f, 1f);
        var light = new Color(Style.Palette.Ivory, Mathf.Lerp(FarMarkerAlpha, 1f, closeness));
        DrawCircle(MarkerPosition.Round(), MarkerRadius, light, filled: true, antialiased: false);
    }
}
