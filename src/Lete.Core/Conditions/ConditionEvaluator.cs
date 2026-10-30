using Lete.Core.Recollections;
namespace Lete.Core.Conditions;

public enum ConditionOutcome
{
    Pass,
    Fail,

    /// <summary>Falha só por um predicado <c>soft</c>: a opção aparece em cinza com <c>hint_if_blocked</c>.</summary>
    NearMiss,
}

/// <summary>Resultado da avaliação; em <see cref="ConditionOutcome.NearMiss"/>, <see cref="Missing"/> é o predicado que faltou.</summary>
public sealed record ConditionResult(ConditionOutcome Outcome, Predicate? Missing = null)
{
    public bool Passed => Outcome == ConditionOutcome.Pass;
}

/// <summary>
/// Avalia condições (DADOS.md 1). <c>NearMiss</c>: a condição falha, mas passaria se exatamente um predicado
/// <c>soft</c> que falhou fosse cumprido (os demais, <c>soft</c> ou não, já bastam).
/// </summary>
public static class ConditionEvaluator
{
    public static ConditionResult Evaluate(Condition condition, IConditionFacts facts)
    {
        if (Holds(condition, facts, assumed: null))
        {
            return new ConditionResult(ConditionOutcome.Pass);
        }

        foreach (var leaf in SoftLeaves(condition).Where(p => !Holds(p, facts, assumed: null)))
        {
            if (Holds(condition, facts, assumed: leaf))
            {
                return new ConditionResult(ConditionOutcome.NearMiss, leaf);
            }
        }

        return new ConditionResult(ConditionOutcome.Fail);
    }

    public static bool Passes(Condition? condition, IConditionFacts facts) =>
        condition is null || Holds(condition, facts, assumed: null);

    /// <summary><paramref name="assumed"/>: um predicado tratado como cumprido (pela referência, não pelo valor).</summary>
    private static bool Holds(Condition condition, IConditionFacts facts, Predicate? assumed) => condition switch
    {
        AllOf all => all.Items.All(c => Holds(c, facts, assumed)),
        AnyOf any => any.Items.Any(c => Holds(c, facts, assumed)),
        NotOf not => !Holds(not.Inner, facts, assumed),
        Predicate p when ReferenceEquals(p, assumed) => true,
        Predicate p => Check(p, facts),
        _ => throw new ArgumentOutOfRangeException(nameof(condition), condition, null),
    };

    private static IEnumerable<Predicate> SoftLeaves(Condition condition) => condition switch
    {
        AllOf all => all.Items.SelectMany(SoftLeaves),
        AnyOf any => any.Items.SelectMany(SoftLeaves),
        NotOf => Enumerable.Empty<Predicate>(),
        Predicate { Soft: true } p => new[] { p },
        _ => Enumerable.Empty<Predicate>(),
    };

    private static bool Check(Predicate predicate, IConditionFacts f) => predicate switch
    {
        OriginIs p => f.Origin == p.Origin,
        GenderIs p => f.Gender == p.Gender,
        RecollectionAt p => f.RecollectionStageOf(p.Id) is { } stage
            && (p.MinStage is null || stage >= p.MinStage)
            && (p.Certainty is null || f.RecollectionCertaintyOf(p.Id) == p.Certainty),
        RecollectionKindFull p => f.IsRecollectionKindFull(p.Kind),
        RecollectionKindFullCount p => f.FullRecollectionKinds >= p.Min,
        HeavyMemoryIs p => f.HeavyMemory(p.Id) == p.State,
        MarkBandIn p => (p.Min is null || f.MarkBand >= p.Min) && (p.Max is null || f.MarkBand <= p.Max),
        TideMin p => f.Tide >= p.Min,
        FlagSet p => f.HasFlag(p.Flag),
        PathKeyOwned p => f.HasPathKey(p.Key),
        OathIsActive p => f.IsOathActive(p.Oath),
        OathIsBroken p => f.IsOathBroken(p.Oath),
        OfferingWasShared p => f.WasOfferingShared(p.Shade, p.Kind),
        ShadeIs p => f.ShadeIs(p.Shade, p.State),
        CompanionIs p => f.Companion == p.Companion,
        BondMin p => f.Bond >= p.Min,
        DifficultyIs p => f.Difficulty == p.Mode,
        ProfileIs p => (p.TopVia is null || f.TopVia == p.TopVia) && (p.TopKind is null || f.TopKind == p.TopKind),
        ItemOwned p => f.OwnsItem(p.Item),
        TriviaIsLit p => f.IsTriviaLit(p.Trivia),
        RegionVisited p => f.WasRegionVisited(p.Region),
        QuestStageMin p => f.QuestStage(p.Quest) is { } stage && stage >= p.Min,
        ShadeLucidityIs p => f.ShadeLucidity == p.Lucidity,
        _ => throw new ArgumentOutOfRangeException(nameof(predicate), predicate, null),
    };
}
