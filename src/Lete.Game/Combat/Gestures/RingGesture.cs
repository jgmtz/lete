using System;
using Godot;
using Lete.Core.Combat.Gestures;
using Lete.Game.UI;

namespace Lete.Game.Combat.Gestures;

/// <summary>
/// Gesto Anel (Atacar): um anel encolhe sobre um círculo fixo; o jogador aperta
/// <c>gesture_confirm</c> quando os dois coincidem. Sem aperto, termina como falho.
/// Depois do aperto o anel fica congelado onde estava, para mostrar o quanto faltou.
/// </summary>
public partial class RingGesture : Control, IGesture
{
    [Export] public float CircleRadius { get; set; } = 90f;
    [Export] public float LineWidth { get; set; } = 6f;

    /// <summary>Falso para pixels duros (quando desenhado dentro do viewport de baixa resolução).</summary>
    [Export] public bool Antialiased { get; set; } = true;

    /// <summary>Centro do círculo em coordenadas do controle; sem valor, usa o meio do controle.</summary>
    public Vector2? Center { get; set; }

    /// <summary>Tempo do anel; precisa ser definido antes de <see cref="Begin"/>.</summary>
    public RingTiming Timing { get; set; } = null!;

    private float _elapsed;
    private float _frozenScale = -1f;
    private bool _ignoreFirstFrame;

    public event Action<float>? Completed;

    public bool IsRunning { get; private set; }

    /// <summary>Segundos desde o começo do anel (para o modo demo apertar no tempo certo).</summary>
    public float Elapsed => _elapsed;

    /// <summary>Quanto o aro do perfeito cresce (em raios do círculo) e em quanto tempo some.</summary>
    [Export] public float BurstGrowth { get; set; } = 0.8f;
    [Export] public float BurstSeconds { get; set; } = 0.4f;

    private float _burst = -1f;

    /// <summary>Resultado perfeito: um aro de ouro sai do círculo, cresce e some.</summary>
    public void PlayPerfectBurst()
    {
        var tween = CreateTween();
        tween.TweenMethod(Callable.From<float>(v =>
        {
            _burst = v;
            QueueRedraw();
        }), 0f, 1f, BurstSeconds);
        tween.TweenCallback(Callable.From(() =>
        {
            _burst = -1f;
            QueueRedraw();
        }));
    }

    /// <summary>Cor do círculo; quem chama pode trocá-la para mostrar o resultado.</summary>
    public Color CircleColor { get; set; } = UiStyle.Palette.Ivory;

    public void Begin()
    {
        _elapsed = 0f;
        _frozenScale = -1f;
        CircleColor = UiStyle.Palette.Ivory;
        IsRunning = true;
        // O aperto que escolheu a ação (ex.: Espaço em "Atacar") não pode contar como gesto.
        _ignoreFirstFrame = true;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!IsRunning)
        {
            return;
        }

        if (_ignoreFirstFrame)
        {
            _ignoreFirstFrame = false;
            QueueRedraw();
            return;
        }

        _elapsed += (float)delta;

        if (Input.IsActionJustPressed("gesture_confirm"))
        {
            Finish(Timing.Score(_elapsed), Timing.ScaleAt(_elapsed));
        }
        else if (Timing.IsExpired(_elapsed))
        {
            Finish(0f, -1f);
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        var center = Center ?? Size / 2f;

        var scale = IsRunning ? Timing.ScaleAt(_elapsed) : _frozenScale;
        if (scale > 0f)
        {
            DrawArc(center, CircleRadius * scale, 0, Mathf.Tau, 128, UiStyle.Palette.Lapis, LineWidth, Antialiased);
        }

        // O círculo por cima: no acerto exato a cor do resultado continua visível.
        DrawArc(center, CircleRadius, 0, Mathf.Tau, 96, CircleColor, LineWidth, Antialiased);

        if (_burst >= 0f)
        {
            var radius = CircleRadius * (1f + BurstGrowth * _burst);
            DrawArc(center, radius, 0, Mathf.Tau, 96, new Color(UiStyle.Palette.Gold, 1f - _burst), LineWidth, Antialiased);
        }
    }

    private void Finish(float score, float frozenScale)
    {
        IsRunning = false;
        _frozenScale = frozenScale;
        Completed?.Invoke(score);
    }
}
