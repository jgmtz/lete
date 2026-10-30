using Lete.Core.Combat;
using Lete.Core.Recollections;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Conditions;

/// <summary>
/// Condição declarativa do conteúdo (DADOS.md 1): combinadores <c>all</c>, <c>any</c>, <c>not</c> e predicados.
/// Lida por <see cref="ConditionParser"/>, avaliada por <see cref="ConditionEvaluator"/>.
/// </summary>
public abstract record Condition;

public sealed record AllOf(IReadOnlyList<Condition> Items) : Condition;

public sealed record AnyOf(IReadOnlyList<Condition> Items) : Condition;

public sealed record NotOf(Condition Inner) : Condition;

/// <summary>
/// Predicado (folha). <see cref="Soft"/>: se for o único que falta, a condição dá <see cref="ConditionOutcome.NearMiss"/>
/// (opção de diálogo em cinza com dica, em vez de escondida).
/// </summary>
public abstract record Predicate : Condition
{
    public bool Soft { get; init; }

    /// <summary>Nome do predicado no JSON (para mensagens e dicas).</summary>
    public abstract string Name { get; }

    /// <summary>Ids de conteúdo que o predicado cita (pasta de destino, id), para a validação no carregamento.</summary>
    public virtual IEnumerable<(string Target, string Id)> References() => Enumerable.Empty<(string, string)>();
}

/// <summary>Estado de uma Memória Pesada para as condições: as duas escolhas ou ainda pendente.</summary>
public enum HeavyMemoryState
{
    Face,
    Leave,
    Pending,
}

/// <summary>
/// O que aconteceu com uma sombra (DADOS.md 1): uma por via de resolução, mais <see cref="Seen"/> (Presenciar).
/// <see cref="Absorbed"/>, <see cref="Fled"/> e <see cref="Yielded"/> (Deixar-se vencer): decisão do João, 29/out/2026.
/// </summary>
public enum ShadeState
{
    Freed,
    Destroyed,
    Spared,
    Seen,
    Absorbed,
    Fled,
    Yielded,
}

public sealed record OriginIs(string Origin) : Predicate
{
    public override string Name => "origin";
    public override IEnumerable<(string, string)> References() => new[] { (ConditionTargets.Origins, Origin) };
}

/// <summary>Só para escolher texto (DADOS.md 1); nunca para abrir ou fechar conteúdo.</summary>
public sealed record GenderIs(Gender Gender) : Predicate
{
    public override string Name => "gender";
}

public sealed record RecollectionAt(string Id, RecollectionStage? MinStage, RecollectionCertainty? Certainty) : Predicate
{
    public override string Name => "recollection";
    public override IEnumerable<(string, string)> References() => new[] { (ConditionTargets.Recollections, Id) };
}

/// <summary>O jogador tem Memória plena de uma Lembrança deste tipo (evolução dos itens pela Afinidade, GDD 6.2).</summary>
public sealed record RecollectionKindFull(RecollectionKind Kind) : Predicate
{
    public override string Name => "recollection_kind_full";
}

public sealed record RecollectionKindFullCount(int Min) : Predicate
{
    public override string Name => "recollection_kind_full_count";
}

public sealed record HeavyMemoryIs(string Id, HeavyMemoryState State) : Predicate
{
    public override string Name => "heavy_memory";
    public override IEnumerable<(string, string)> References() => new[] { (ConditionTargets.Recollections, Id) };
}

public sealed record MarkBandIn(MarkBand? Min, MarkBand? Max) : Predicate
{
    public override string Name => "mark_band";
}

public sealed record TideMin(int Min) : Predicate
{
    public override string Name => "tide_min";
}

public sealed record FlagSet(string Flag) : Predicate
{
    public override string Name => "flag";
}

public sealed record PathKeyOwned(string Key) : Predicate
{
    public override string Name => "path_key";
    public override IEnumerable<(string, string)> References() => new[] { (ConditionTargets.PathKeys, Key) };
}

public sealed record OathIsActive(string Oath) : Predicate
{
    public override string Name => "oath_active";
    public override IEnumerable<(string, string)> References() => new[] { (ConditionTargets.Oaths, Oath) };
}

public sealed record OathIsBroken(string Oath) : Predicate
{
    public override string Name => "oath_broken";
    public override IEnumerable<(string, string)> References() => new[] { (ConditionTargets.Oaths, Oath) };
}

/// <summary>Oferenda partilhada com uma sombra; sem <see cref="Kind"/>, qualquer tipo serve.</summary>
public sealed record OfferingWasShared(string Shade, string? Kind) : Predicate
{
    public override string Name => "offering_shared";
    public override IEnumerable<(string, string)> References() => new[] { (ConditionTargets.Shades, Shade) };
}

public sealed record ShadeIs(string Shade, ShadeState State) : Predicate
{
    public override string Name => "shade_state";
    public override IEnumerable<(string, string)> References() => new[] { (ConditionTargets.Shades, Shade) };
}

public sealed record CompanionIs(string Companion) : Predicate
{
    public override string Name => "companion";
    public override IEnumerable<(string, string)> References() => new[] { (ConditionTargets.Companions, Companion) };
}

public sealed record BondMin(int Min) : Predicate
{
    public override string Name => "bond_min";
}

public sealed record DifficultyIs(string Mode) : Predicate
{
    public override string Name => "difficulty";
    public override IEnumerable<(string, string)> References() => new[] { (ConditionTargets.Difficulty, Mode) };
}

/// <summary>Perfil do jogador (recente): a via mais usada e/ou o tipo de Memória predominante.</summary>
public sealed record ProfileIs(Via? TopVia, RecollectionKind? TopKind) : Predicate
{
    public override string Name => "profile";
}

public sealed record ItemOwned(string Item) : Predicate
{
    public override string Name => "item_owned";
    public override IEnumerable<(string, string)> References() => new[] { (ConditionTargets.Items, Item) };
}

public sealed record TriviaIsLit(string Trivia) : Predicate
{
    public override string Name => "trivia_lit";
}

public sealed record RegionVisited(string Region) : Predicate
{
    public override string Name => "region_visited";
    public override IEnumerable<(string, string)> References() => new[] { (ConditionTargets.Regions, Region) };
}

/// <summary>Missão pelo menos neste estágio (índice na lista <c>stages</c> da missão).</summary>
public sealed record QuestStageMin(string Quest, int Min) : Predicate
{
    public override string Name => "quest_stage";
    public override IEnumerable<(string, string)> References() => new[] { (ConditionTargets.Missions, Quest) };
}

/// <summary>Estado de lucidez da sombra com quem se fala (só dentro de um diálogo, DADOS.md 5).</summary>
public sealed record ShadeLucidityIs(string Lucidity) : Predicate
{
    public override string Name => "shade_lucidity";
}

/// <summary>Conjuntos de ids que as condições podem citar (pastas de <c>content/</c> e listas de ids).</summary>
public static class ConditionTargets
{
    public const string Origins = "origins";
    public const string Recollections = "recollections";
    public const string Oaths = "oaths";
    public const string Shades = "shades";
    public const string Companions = "companheiros do elenco";
    public const string Difficulty = "difficulty";
    public const string Items = "items";
    public const string Regions = "regions";
    public const string Missions = "missions";
    public const string PathKeys = "path";
}
