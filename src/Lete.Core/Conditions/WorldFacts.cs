using Lete.Core.Recollections;
using Lete.Core.Combat;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Conditions;

/// <summary>
/// Responde às condições pelo <see cref="WorldState"/>. Os predicados de sistemas que ainda não existem (missões)
/// lançam <see cref="NotSupportedException"/> dizendo qual sistema falta, em vez de responder errado em silêncio.
/// </summary>
public sealed class WorldFacts : IConditionFacts
{
    private readonly WorldState _state;
    private readonly MarkRules _markRules;
    private readonly Mosaic _mosaic;
    private readonly Profile _profile;

    public WorldFacts(WorldState state, MarkRules markRules, Mosaic mosaic, Profile profile)
    {
        _state = state;
        _markRules = markRules;
        _mosaic = mosaic;
        _profile = profile;
    }

    public string? Origin => _state.OriginId;
    public Gender Gender => _state.Gender;
    public string? Companion => _state.CompanionId;
    public MarkBand MarkBand => _markRules.BandOf(_state.Mark);
    public int Tide => TideTracker.StepOf(_state.Tide);
    public int FullRecollectionKinds => _mosaic.FullKinds;
    public RecollectionStage? RecollectionStageOf(string id) => _mosaic.StageOf(id);
    public RecollectionCertainty? RecollectionCertaintyOf(string id) => _mosaic.CertaintyOf(id);

    /// <summary>Lucidez da sombra do diálogo aberto (quem conduz o diálogo preenche; nula fora dele).</summary>
    public string? ShadeLucidity { get; set; }

    public bool HasFlag(string flag) => _state.Flags.Contains(flag);

    public HeavyMemoryState HeavyMemory(string id) => _state.HeavyMemories.TryGetValue(id, out var choice)
        ? choice == HeavyMemoryChoice.Face ? HeavyMemoryState.Face : HeavyMemoryState.Leave
        : HeavyMemoryState.Pending;

    public string? Difficulty => _state.Difficulty;
    public int Bond => _state.CompanionBond;
    public Via? TopVia => _profile.TopVia;
    public RecollectionKind? TopKind => _profile.TopKind;
    public bool HasPathKey(string key) => _state.PathKeys.ContainsKey(key);
    public bool IsOathActive(string oath) => _state.Oaths.TryGetValue(oath, out var status) && status == OathStatus.Active;
    public bool IsOathBroken(string oath) => _state.Oaths.TryGetValue(oath, out var status) && status == OathStatus.Broken;
    public bool WasOfferingShared(string shade, string? kind) => kind is not null
        ? _state.SharedOfferings.Contains($"{shade}/{kind}")
        : _state.SharedOfferings.Any(s => s.StartsWith($"{shade}/", StringComparison.Ordinal));

    public bool ShadeIs(string shade, ShadeState state) => _state.Shades.TryGetValue(shade, out var s) && s == state;
    public bool OwnsItem(string item) => _state.Items.Owned.ContainsKey(item);
    public bool IsRecollectionKindFull(RecollectionKind kind) => _mosaic.IsKindFull(kind);
    public bool IsTriviaLit(string trivia) => _state.LitTrivias.Contains(trivia);
    public bool WasRegionVisited(string region) => _state.VisitedRegions.Contains(region);
    public int? QuestStage(string quest) => Missing<int?>("quest_stage", "missões");

    private static T Missing<T>(string predicate, string system) =>
        throw new NotSupportedException($"O predicado '{predicate}' depende de {system}, que ainda não existe no estado (M2).");
}
