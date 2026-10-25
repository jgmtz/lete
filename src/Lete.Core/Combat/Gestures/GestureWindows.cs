namespace Lete.Core.Combat.Gestures;

/// <summary>
/// Limiares que transformam o valor do gesto (0 a 1, proximidade do ideal) em perfeito/bom/falho.
/// Vêm do modo de dificuldade (<c>content/difficulty</c>); o modo só ajusta estes limiares.
/// </summary>
public sealed record GestureWindows
{
    public GestureWindows(float perfect, float good)
    {
        if (good is < 0f or > 1f || perfect is < 0f or > 1f || perfect < good)
        {
            throw new ArgumentException($"Limiares inválidos: perfeito {perfect}, bom {good} (0 <= bom <= perfeito <= 1).");
        }

        Perfect = perfect;
        Good = good;
    }

    public float Perfect { get; }
    public float Good { get; }

    /// <summary>Janelas mais largas (ex.: Postura do Aulo): os dois limiares descem <paramref name="amount"/>.</summary>
    public GestureWindows Widened(float amount) =>
        new(Math.Max(0f, Perfect - amount), Math.Max(0f, Good - amount));

    public GestureGrade Grade(float score) =>
        score >= Perfect ? GestureGrade.Perfect
        : score >= Good ? GestureGrade.Good
        : GestureGrade.Failed;
}
