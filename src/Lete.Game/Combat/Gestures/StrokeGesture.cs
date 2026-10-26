using System;
using Godot;
using Lete.Core.Combat.Gestures;

namespace Lete.Game.Combat.Gestures;

/// <summary>
/// Gesto Traço (Memória, GDD 4.4): uma fileira de 2 a 4 setas; um quadro dourado fecha sobre a seta da vez no
/// pulso, e o jogador aperta aquela direção nesse instante (diagonal = duas direções juntas). A regra fica em
/// <see cref="StrokeRun"/>; aqui só entrada e desenho, em pixels da arte.
/// </summary>
public partial class StrokeGesture : Control, IGesture
{
    [Export] public BattleHudStyle Style { get; set; } = null!;

    /// <summary>Lado de cada casa de seta, espaço entre casas e quanto o quadro do pulso começa maior (pixels da arte).</summary>
    [Export] public int CellSize { get; set; } = 13;
    [Export] public int CellGap { get; set; } = 4;
    [Export] public float PulseStartGrow { get; set; } = 6f;

    /// <summary>Tempo para juntar duas teclas numa diagonal (o instante do aperto é o da primeira tecla).</summary>
    [Export] public float ChordSeconds { get; set; } = 0.07f;

    [Export] public float BoxAlpha { get; set; } = 0.8f;

    /// <summary>Centro da fileira em coordenadas do controle; sem valor, usa o meio do controle.</summary>
    public Vector2? Center { get; set; }

    /// <summary>A sequência; precisa ser definida antes de <see cref="Begin"/>.</summary>
    public StrokePattern Pattern { get; set; } = null!;

    private StrokeRun? _run;
    private float _chordLeft = -1f;
    private float _chordAt;
    private Vector2I _chordAxes;

    public event Action<float>? Completed;

    public bool IsRunning { get; private set; }

    /// <summary>Segundos desde o começo (para o modo demo apertar no pulso).</summary>
    public float Elapsed => _run?.Elapsed ?? 0f;

    public void Begin()
    {
        _run = new StrokeRun(Pattern);
        _chordLeft = -1f;
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

        var dt = (float)delta;
        var axes = PressedAxes();
        var newPress = Input.IsActionJustPressed("move_left") || Input.IsActionJustPressed("move_right")
            || Input.IsActionJustPressed("move_up") || Input.IsActionJustPressed("move_down");

        if (_chordLeft < 0f && newPress)
        {
            _chordLeft = ChordSeconds;
            _chordAt = _run.Elapsed;
            _chordAxes = axes;
        }
        else if (_chordLeft >= 0f)
        {
            // Junta o que for apertado durante a janela (a segunda tecla de uma diagonal).
            _chordAxes = new Vector2I(_chordAxes.X != 0 ? _chordAxes.X : axes.X, _chordAxes.Y != 0 ? _chordAxes.Y : axes.Y);
            _chordLeft -= dt;
            if (_chordLeft < 0f && StrokeRun.FromAxes(_chordAxes.X, _chordAxes.Y) is { } direction)
            {
                _run.Press(direction, _chordAt);
            }
        }

        _run.Advance(dt);
        if (_run.IsFinished)
        {
            IsRunning = false;
            Completed?.Invoke(_run.Score);
        }

        QueueRedraw();
    }

    private static Vector2I PressedAxes() => new(
        (Input.IsActionPressed("move_right") ? 1 : 0) - (Input.IsActionPressed("move_left") ? 1 : 0),
        (Input.IsActionPressed("move_down") ? 1 : 0) - (Input.IsActionPressed("move_up") ? 1 : 0));

    public override void _Draw()
    {
        if (_run is null)
        {
            return;
        }

        var p = Style.Palette;
        var steps = Pattern.Steps.Count;
        var width = steps * CellSize + (steps - 1) * CellGap;
        var origin = ((Center ?? Size / 2f) - new Vector2(width, CellSize) / 2f).Round();
        GestureFrame.Draw(this, new Rect2(origin, new Vector2(width, CellSize)).Grow(CellGap), BoxAlpha);

        for (var i = 0; i < steps; i++)
        {
            var cell = new Rect2(origin + new Vector2(i * (CellSize + CellGap), 0), new Vector2(CellSize, CellSize));
            var done = i < _run.StepScores.Count;
            var color = done ? (_run.StepScores[i] > 0f ? p.Gold : p.ClayGray)
                : i == _run.CurrentStep ? p.Ivory
                : p.IvoryDim;
            DrawRect(cell, color, filled: false);
            DrawArrow(cell, Pattern.Steps[i], color);

            // O quadro do pulso fecha sobre a casa da vez e encosta nela no instante certo.
            if (!done && i == _run.CurrentStep)
            {
                var toBeat = Mathf.Clamp((Pattern.IdealTime(i) - _run.Elapsed) / Pattern.BeatSeconds, 0f, 1f);
                DrawRect(cell.Grow(Mathf.Round(PulseStartGrow * toBeat) + 1f), p.Gold, filled: false);
            }
        }
    }

    private void DrawArrow(Rect2 cell, StrokeDirection direction, Color color)
    {
        var dir = (direction switch
        {
            StrokeDirection.Up => new Vector2(0, -1),
            StrokeDirection.UpRight => new Vector2(1, -1),
            StrokeDirection.Right => new Vector2(1, 0),
            StrokeDirection.DownRight => new Vector2(1, 1),
            StrokeDirection.Down => new Vector2(0, 1),
            StrokeDirection.DownLeft => new Vector2(-1, 1),
            StrokeDirection.Left => new Vector2(-1, 0),
            _ => new Vector2(-1, -1),
        }).Normalized();

        var center = cell.GetCenter().Floor() + new Vector2(0.5f, 0.5f);
        var reach = (CellSize - 4) / 2f;
        var tip = center + dir * reach;
        var tail = center - dir * reach;
        var head = reach * 0.6f;
        DrawLine(tail.Round(), tip.Round(), color, 1f, antialiased: false);
        DrawLine(tip.Round(), (tip - dir.Rotated(Mathf.Pi / 4f) * head).Round(), color, 1f, antialiased: false);
        DrawLine(tip.Round(), (tip - dir.Rotated(-Mathf.Pi / 4f) * head).Round(), color, 1f, antialiased: false);
    }
}
