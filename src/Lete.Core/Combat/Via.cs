namespace Lete.Core.Combat;

/// <summary>
/// Via que encerrou um encontro (GDD 4.5), como o Perfil a conta. No conteúdo e no save, em snake_case:
/// <c>fight</c>, <c>free</c>, <c>absorb</c>, <c>witness</c>, <c>spare</c>, <c>flee</c>, <c>yield</c>.
/// </summary>
public enum Via
{
    Fight,
    Free,
    Absorb,
    Witness,
    Spare,
    Flee,

    /// <summary>Deixar-se vencer (a ação escolhida, não a derrota).</summary>
    Yield,
}
