using Lete.Core.Combat;

namespace Lete.Core.Systems;

/// <summary>Como o companheiro vê uma via (HISTORIA 5.1).</summary>
public enum Approval
{
    Approve,
    Tolerate,
    Reprove,
}

/// <summary>O que o apoio do companheiro faz na luta (GDD 4.10, HISTORIA 5.2).</summary>
public enum SupportEffect
{
    /// <summary>Postura (Aulo): alarga as janelas do aparo e da esquiva no golpe seguinte da sombra.</summary>
    WidenDefense,

    /// <summary>Lamento (Ismena): alarga o próximo Escutar e, sem o nome, sopra a pista dele.</summary>
    WidenListen,

    /// <summary>Surrupio (Dimas): deixa absorver com mais Firmeza na próxima ação e acha óbolos.</summary>
    EaseAbsorb,

    /// <summary>Brasa (Septímia): a próxima Memória custa menos Lucidez.</summary>
    CheaperMemory,

    /// <summary>Canto (Cálias): a legenda mostra também o golpe depois do próximo.</summary>
    Foretell,

    /// <summary>Esboço (Lia, proposta): o próximo Traço tem janelas mais largas.</summary>
    WidenStroke,
}

/// <summary>
/// Um companheiro (<c>content/companions/&lt;id&gt;.json</c>): o apoio, a força dele (<paramref name="Power"/>, o que o
/// efeito usa: alargamento, fração, desconto), os óbolos que o Surrupio acha, a aprovação por via (o que não está no
/// mapa é tolerado) e as falas: a relutante (uma vez por luta) e as das conversas na Trívia, em ordem.
/// </summary>
public sealed record Companion(
    string Id,
    string NameKey,
    string SupportId,
    SupportEffect Effect,
    float Power,
    int FoundObols,
    IReadOnlyDictionary<Via, Approval> Approvals,
    string ReluctantKey,
    IReadOnlyList<string> TalkKeys)
{
    public Approval ApprovalOf(Via via) => Approvals.TryGetValue(via, out var approval) ? approval : Approval.Tolerate;

    public IEnumerable<string> Validate()
    {
        if (Power is < 0f or > 1f)
        {
            yield return $"Força do apoio fora de 0..1 (veio {Power}).";
        }

        if (FoundObols < 0)
        {
            yield return "Óbolos achados não podem ser negativos.";
        }

        if (TalkKeys.Count == 0)
        {
            yield return "O companheiro precisa de pelo menos uma conversa de Trívia.";
        }
    }
}

/// <summary>De onde vem o vínculo (GDD 4.10): partilhar oferendas, lutar juntos, conversar e responder aos comentários.</summary>
public enum BondSource
{
    OfferingShared,
    Battle,
    TriviaTalk,
    CommentAgreed,
    CommentDisagreed,
}

/// <summary>
/// Regras do vínculo (<c>content/rules/companheiro.json</c>, decisões do João de 30/out/2026): o vínculo vai de 0 a
/// <paramref name="BondMax"/> e só cresce; a partir de <paramref name="FastBond"/> o apoio vem a cada
/// <paramref name="FastIntervalTurns"/> turnos em vez de <paramref name="IntervalTurns"/>. O apoio relutante (depois de
/// uma via que ele reprova) vale <paramref name="ReluctantFactor"/> do normal.
/// </summary>
public sealed record CompanionRules(
    int BondMax,
    int FastBond,
    int IntervalTurns,
    int FastIntervalTurns,
    float ReluctantFactor,
    IReadOnlyDictionary<BondSource, int> Gains)
{
    public int Gain(BondSource source) => Gains.TryGetValue(source, out var gain) ? gain : 0;

    public IEnumerable<string> Validate()
    {
        if (BondMax < 1 || FastBond < 0 || FastBond > BondMax)
        {
            yield return $"Vínculo: máximo {BondMax} e faixa rápida {FastBond} inválidos.";
        }

        if (IntervalTurns < 1 || FastIntervalTurns < 1 || FastIntervalTurns > IntervalTurns)
        {
            yield return "Intervalos do apoio precisam ser pelo menos 1, e o rápido não pode ser maior que o normal.";
        }

        if (ReluctantFactor is < 0f or > 1f)
        {
            yield return $"Fator do apoio relutante fora de 0..1 (veio {ReluctantFactor}).";
        }

        if (Gains.Values.Any(g => g < 0))
        {
            yield return "O vínculo só cresce: nenhum ganho pode ser negativo.";
        }
    }
}
