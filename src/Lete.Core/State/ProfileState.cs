using Lete.Core.Combat;

namespace Lete.Core.State;

/// <summary>
/// Os fatos do Perfil (GDD 2.5) no <see cref="WorldState"/>: totais desde o começo (para o final) e os últimos
/// encontros (o comportamento recente; o tamanho da janela está em <c>content/rules/perfil.json</c>).
/// </summary>
public sealed class ProfileState
{
    public Dictionary<Via, int> ViaTotals { get; } = new();

    /// <summary>Usos de Memória em combate por tipo de Lembrança.</summary>
    public Dictionary<RecollectionKind, int> KindTotals { get; } = new();

    /// <summary>Os últimos encontros, do mais antigo ao mais novo.</summary>
    public List<ProfileEncounter> Recent { get; } = new();
}

/// <summary>
/// Um encontro encerrado: a via do desfecho (nula numa derrota, que não é via) e o tipo de cada Memória usada nele.
/// </summary>
public sealed record ProfileEncounter(Via? Via, IReadOnlyList<RecollectionKind> Kinds);
