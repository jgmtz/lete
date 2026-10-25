namespace Lete.Core.Combat;

/// <summary>
/// Firmeza de uma sombra (a "vida" dela, GDD 4.2). O jogador só vê a fração numa barra, nunca o número.
/// </summary>
public sealed class ShadeResolve
{
    public ShadeResolve(float max)
    {
        if (max <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(max), "A Firmeza máxima precisa ser positiva.");
        }

        Max = max;
        Current = max;
    }

    public float Max { get; }
    public float Current { get; private set; }

    /// <summary>Fração de 0 a 1, para a barra.</summary>
    public float Fraction => Current / Max;

    public bool IsBroken => Current <= 0f;

    public void Reduce(float amount)
    {
        if (amount < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Use um valor positivo para reduzir a Firmeza.");
        }

        Current = Math.Max(0f, Current - amount);
    }
}
