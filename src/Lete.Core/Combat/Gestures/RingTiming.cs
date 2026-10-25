namespace Lete.Core.Combat.Gestures;

/// <summary>
/// Gesto Anel: um anel encolhe sobre um círculo fixo e o jogador aperta quando coincidem.
/// O valor do gesto cai linearmente com a distância (em segundos) entre o aperto e o encontro.
/// </summary>
/// <param name="ShrinkSeconds">Tempo do anel do tamanho inicial até coincidir com o círculo.</param>
/// <param name="StartScale">Tamanho inicial do anel em relação ao círculo (ex.: 3 = três vezes maior).</param>
/// <param name="ZeroScoreOffsetSeconds">Distância do encontro em que o valor chega a zero.</param>
public sealed record RingTiming(float ShrinkSeconds, float StartScale, float ZeroScoreOffsetSeconds)
{
    /// <summary>Depois deste instante sem aperto, o gesto termina como falho.</summary>
    public float ExpiresAt => ShrinkSeconds + ZeroScoreOffsetSeconds;

    /// <summary>Tamanho do anel em relação ao círculo; continua encolhendo depois do encontro.</summary>
    public float ScaleAt(float elapsed) =>
        Math.Max(0f, StartScale + (1f - StartScale) * elapsed / ShrinkSeconds);

    /// <summary>Valor de 0 a 1 de um aperto feito em <paramref name="pressedAt"/> segundos.</summary>
    public float Score(float pressedAt)
    {
        var distance = Math.Abs(pressedAt - ShrinkSeconds);
        return Math.Clamp(1f - distance / ZeroScoreOffsetSeconds, 0f, 1f);
    }

    public bool IsExpired(float elapsed) => elapsed > ExpiresAt;
}
