namespace Lete.Core.Combat;

/// <summary>
/// Um recurso com máximo (Fôlego e Lucidez do protagonista, GDD 4.2). O jogador só vê a fração numa barra.
/// </summary>
public sealed class Gauge
{
    public Gauge(float max)
    {
        if (max <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(max), "O máximo precisa ser positivo.");
        }

        Max = max;
        Current = max;
    }

    public float Max { get; }
    public float Current { get; private set; }

    /// <summary>Fração de 0 a 1, para a barra.</summary>
    public float Fraction => Current / Max;

    public bool IsEmpty => Current <= 0f;

    public void Reduce(float amount)
    {
        if (amount < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Use um valor positivo para reduzir.");
        }

        Current = Math.Max(0f, Current - amount);
    }

    public void Restore(float amount)
    {
        if (amount < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Use um valor positivo para recuperar.");
        }

        Current = Math.Min(Max, Current + amount);
    }

    public void Refill() => Current = Max;
}
