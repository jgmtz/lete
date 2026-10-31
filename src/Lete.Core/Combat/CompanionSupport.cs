using Lete.Core.Systems;

namespace Lete.Core.Combat;

/// <summary>
/// Apoio do companheiro numa luta (GDD 4.10): age a cada <paramref name="IntervalTurns"/> turnos (o vínculo encurta) e
/// faz <paramref name="Effect"/> com a força <paramref name="Power"/>. Depois de uma via que ele reprova, o apoio
/// seguinte sai relutante, com <paramref name="ReluctantFactor"/> da força.
/// </summary>
public sealed record CompanionSupport(
    string CompanionId,
    string SupportId,
    SupportEffect Effect,
    int IntervalTurns,
    float Power,
    float ReluctantFactor = 1f)
{
    /// <summary>A Postura vale no golpe seguinte da sombra; os outros apoios, na próxima ação do jogador.</summary>
    public bool LastsUntilShadeTurnEnds => Effect == SupportEffect.WidenDefense;

    public IEnumerable<string> Validate()
    {
        if (IntervalTurns < 1)
        {
            yield return $"Intervalo do apoio precisa ser pelo menos 1 turno (veio {IntervalTurns}).";
        }

        if (Power is < 0f or > 1f)
        {
            yield return $"Força do apoio fora de 0..1 (veio {Power}).";
        }

        if (ReluctantFactor is < 0f or > 1f)
        {
            yield return $"Fator do apoio relutante fora de 0..1 (veio {ReluctantFactor}).";
        }
    }
}
