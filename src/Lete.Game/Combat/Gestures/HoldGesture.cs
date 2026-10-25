using System;
using Godot;
using Lete.Core.Combat.Gestures;

namespace Lete.Game.Combat.Gestures;

/// <summary>
/// Gesto Segurar (Escutar, Presenciar, Suportar; GDD 4.4): uma barra fina enche enquanto <c>gesture_confirm</c>
/// fica apertado; soltar perto da marca dourada dá o melhor valor. A regra fica em <see cref="HoldTiming"/>;
/// aqui só entrada e desenho, em pixels da arte.
/// </summary>
public partial class HoldGesture : Control, IGesture
{
    [Export] public BattleHudStyle Style { get; set; } = null!;

    /// <summary>Tamanho da barra e altura da marca do ponto certo, em pixels da arte.</summary>
    [Export] public Vector2I BarSize { get; set; } = new(96, 5);
    [Export] public int TargetMarkHeight { get; set; } = 9;

    /// <summary>Centro da barra em coordenadas do controle; sem valor, usa o meio do controle.</summary>
    public Vector2? Center { get; set; }

    /// <summary>Tempo do gesto; precisa ser definido antes de <see cref="Begin"/>.</summary>
    public HoldTiming Timing { get; set; } = null!;

    private float _waited;
    private float _held;
    private bool _holding;
    private bool _awaitRelease;
    private float _frozenFill = -1f;

    public event Action<float>? Completed;

    public bool IsRunning { get; private set; }

    public void Begin()
    {
        _waited = 0f;
        _held = 0f;
        _holding = false;
        _frozenFill = -1f;
        // O aperto que escolheu a ação (ex.: Espaço em "Escutar") não pode começar a encher a barra.
        _awaitRelease = Input.IsActionPressed("gesture_confirm");
        IsRunning = true;
        Visible = true;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!IsRunning)
        {
            return;
        }

        var dt = (float)delta;
        if (_awaitRelease)
        {
            _awaitRelease = Input.IsActionPressed("gesture_confirm");
        }
        else if (!_holding)
        {
            _waited += dt;
            _holding = Input.IsActionJustPressed("gesture_confirm");
            if (!_holding && _waited >= Timing.WaitSeconds)
            {
                Finish(0f);
            }
        }
        else
        {
            _held += dt;
            if (!Input.IsActionPressed("gesture_confirm") || Timing.IsFull(_held))
            {
                Finish(Timing.FillAt(_held));
            }
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Timing is null)
        {
            return;
        }

        var size = new Vector2(BarSize.X, BarSize.Y);
        var origin = ((Center ?? Size / 2f) - size / 2f).Round();
        var bar = new Rect2(origin, size);
        var fill = IsRunning ? Timing.FillAt(_held) : Mathf.Max(0f, _frozenFill);

        DrawRect(bar, Style.Palette.Night);
        DrawRect(new Rect2(origin, new Vector2(Mathf.Round(size.X * fill), size.Y)), Style.Palette.Ivory);
        DrawRect(bar.Grow(1f), Style.Palette.Gold, filled: false);

        var targetX = origin.X + Mathf.Round(size.X * Timing.Target);
        var markTop = origin.Y + (size.Y - TargetMarkHeight) / 2f;
        DrawRect(new Rect2(targetX, Mathf.Round(markTop), 1f, TargetMarkHeight), Style.Palette.Gold);
    }

    private void Finish(float fill)
    {
        IsRunning = false;
        _frozenFill = fill;
        Completed?.Invoke(fill <= 0f ? 0f : Timing.Score(fill));
    }
}
