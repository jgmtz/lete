namespace Lete.Core.Combat.Gestures;

/// <summary>
/// Gesto Segurar (GDD 4.4): enquanto o botão fica apertado, uma barra enche de 0 a 1 em
/// <paramref name="FillSeconds"/>; o valor vem de quão perto de <paramref name="Target"/> o jogador soltou.
/// Encher até o fim sem soltar conta como soltar no 1. Sem apertar em <paramref name="WaitSeconds"/>, é falho.
/// </summary>
public sealed record HoldTiming(float FillSeconds, float Target, float ZeroScoreDistance, float WaitSeconds)
{
    public float FillAt(float heldSeconds) => Math.Clamp(heldSeconds / FillSeconds, 0f, 1f);

    public bool IsFull(float heldSeconds) => heldSeconds >= FillSeconds;

    public float Score(float fill) => Math.Max(0f, 1f - Math.Abs(fill - Target) / ZeroScoreDistance);

    public IEnumerable<string> Validate()
    {
        if (FillSeconds <= 0f || ZeroScoreDistance <= 0f || WaitSeconds <= 0f)
        {
            yield return "Tempo de encher, distância que zera e espera precisam ser positivos.";
        }

        if (Target is <= 0f or >= 1f)
        {
            yield return $"O ponto certo precisa ficar entre 0 e 1 (veio {Target}).";
        }
    }
}
