namespace Lete.Core.Systems;

/// <summary>Faixas da Marca (GDD 2.3): baixa (nada acontece), média e alta.</summary>
public enum MarkBand
{
    Low,
    Mid,
    High,
}

/// <summary>
/// Números da Marca (<c>content/rules/marca.json</c>, todos provisórios): onde começam as faixas média e alta e
/// quantas Memórias Pesadas encaradas devolvem o rosto pela regra C (PENDÊNCIAS 44); quanto sobe por perjúrio.
/// </summary>
public sealed record MarkRules(int MidFrom, int HighFrom, int FacedToReturnFace, int PerjuryMark = 0)
{
    public MarkBand BandOf(int mark) => mark >= HighFrom ? MarkBand.High : mark >= MidFrom ? MarkBand.Mid : MarkBand.Low;

    public IEnumerable<string> Validate()
    {
        if (MidFrom <= 0 || HighFrom <= MidFrom)
        {
            yield return "As faixas da Marca precisam ser 0 < média < alta.";
        }

        if (FacedToReturnFace <= 0)
        {
            yield return "O mínimo de Pesadas encaradas para devolver o rosto precisa ser positivo.";
        }

        if (PerjuryMark < 0)
        {
            yield return "A Marca do perjúrio não pode ser negativa.";
        }
    }
}
