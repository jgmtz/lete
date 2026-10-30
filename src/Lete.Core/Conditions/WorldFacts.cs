using Lete.Core.Recollections;
using Lete.Core.Combat;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Conditions;

/// <summary>
/// Responde às condições pelo <see cref="WorldState"/>. Os predicados de sistemas que ainda não existem (oferendas, sombras, vínculo, itens, Trívias, regiões, missões, dificuldade)
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

    public string? Difficulty => Missing<string?>("difficulty", "modo de dificuldade no estado");
    public int Bond => Missing<int>("bond_min", "vínculo do companheiro");
    public Via? TopVia => _profile.TopVia;
    public RecollectionKind? TopKind => _profile.TopKind;
    public bool HasPathKey(string key) => _state.PathKeys.ContainsKey(key);
    public bool IsOathActive(string oath) => _state.Oaths.TryGetValue(oath, out var status) && status == OathStatus.Active;
    public bool IsOathBroken(string oath) => _state.Oaths.TryGetValue(oath, out var status) && status == OathStatus.Broken;
    public bool WasOfferingShared(string shade, string? kind) => Missing<bool>("offering_shared", "oferendas");
    public bool ShadeIs(string shade, ShadeState state) => Missing<bool>("shade_state", "estado das sombras");
    public bool OwnsItem(string item) => Missing<bool>("item_owned", "itens");
    public bool IsTriviaLit(string trivia) => Missing<bool>("trivia_lit", "Trívias");
    public bool WasRegionVisited(string region) => Missing<bool>("region_visited", "regiões visitadas");
    public int? QuestStage(string quest) => Missing<int?>("quest_stage", "missões");

    private static T Missing<T>(string predicate, string system) =>
        throw new NotSupportedException($"O predicado '{predicate}' depende de {system}, que ainda não existe no estado (M2).");
}
