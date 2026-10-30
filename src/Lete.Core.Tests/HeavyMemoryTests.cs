using Lete.Core.Combat;
using Lete.Core.Conditions;
using Lete.Core.Data;
using Lete.Core.Events;
using Lete.Core.Recollections;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Tests;

public class HeavyMemoryTests
{
    private readonly WorldState _state = new();
    private readonly EventBus _bus = new();
    private readonly List<object> _events = new();

    public HeavyMemoryTests() => _bus.AnyPublished += e => _events.Add(e);

    /// <summary>Pesadas <c>pesada_1</c> a <c>pesada_N</c>, na ordem, cada uma com a Culpa <c>culpa_N</c>.</summary>
    public static IReadOnlyList<Recollection> Queue(int count, Condition? gateOfFirst = null) =>
        Enumerable.Range(1, count).Select(i => Heavy(i, i == 1 ? gateOfFirst : null)).ToList();

    private static Recollection Heavy(int order, Condition? gate = null) => new(
        $"pesada_{order}", RecollectionKind.Family, RecollectionScope.Heavy, null, $"pesada.{order}.titulo",
        new Dictionary<RecollectionStage, string> { [RecollectionStage.Full] = $"pesada.{order}.verdade" },
        $"culpa_{order}", Array.Empty<RecollectionSource>(), Array.Empty<string>(), order, 9,
        new HeavyMemory(order, gate, "aviso", "aflora", "deixar", new Lightness(10f * order, 1)));

    [Fact]
    public void Queue_IsFixed_OnlyTheNextCanBeChosen()
    {
        var heavy = new HeavyMemories(_state, _bus, Queue(3));
        Assert.Equal("pesada_1", heavy.Next!.Id);
        Assert.Throws<InvalidOperationException>(() => heavy.Choose("pesada_2", HeavyMemoryChoice.Face));
        Assert.Throws<ArgumentException>(() => heavy.Choose("nao_existe", HeavyMemoryChoice.Face));

        heavy.Choose("pesada_1", HeavyMemoryChoice.Face);
        heavy.Choose("pesada_2", HeavyMemoryChoice.Leave);
        Assert.Equal("pesada_3", heavy.Next!.Id);
        heavy.Choose("pesada_3", HeavyMemoryChoice.Leave);
        Assert.Null(heavy.Next);
        Assert.Contains(new HeavyMemoryChosen("pesada_2", HeavyMemoryChoice.Leave), _events);
    }

    [Fact]
    public void Queue_RejectsRepeatedOrMissingOrder()
    {
        Assert.Throws<FormatException>(() => new HeavyMemories(_state, _bus, new[] { Heavy(1), Heavy(3) }));
        Assert.Throws<FormatException>(() => new HeavyMemories(_state, _bus, new[] { Heavy(1), Heavy(1) with { Id = "outra" } }));
    }

    [Fact]
    public void Surfaces_OnlyAsTheNextAndWithItsGate()
    {
        var heavy = new HeavyMemories(_state, _bus, Queue(2, new FlagSet("chegou_ao_portao")));
        var facts = new WorldFacts(_state, new MarkRules(4, 8, 4), Mosaic(heavy.Queue), new Profile(_state, _bus, ProfileTests.Rules));

        Assert.False(heavy.CanSurface("pesada_1", facts));
        _state.Flags.Add("chegou_ao_portao");
        Assert.True(heavy.CanSurface("pesada_1", facts));
        Assert.False(heavy.CanSurface("pesada_2", facts));

        heavy.Choose("pesada_1", HeavyMemoryChoice.Face);
        Assert.False(heavy.CanSurface("pesada_1", facts));
        Assert.True(heavy.CanSurface("pesada_2", facts));
    }

    [Fact]
    public void Facing_MakesItFullInTheMosaicWithItsGuilt_LeavingCoversIt()
    {
        var heavy = new HeavyMemories(_state, _bus, Queue(2));
        using var mosaic = Mosaic(heavy.Queue);
        var skills = new[] { Skill("culpa_1"), Skill("culpa_2") };

        heavy.Choose("pesada_1", HeavyMemoryChoice.Face);
        heavy.Choose("pesada_2", HeavyMemoryChoice.Leave);

        Assert.Equal(RecollectionStage.Full, mosaic.StageOf("pesada_1"));
        Assert.Contains(new RecollectionGained("pesada_1", RecollectionStage.Full), _events);
        var guilt = Assert.Single(mosaic.CombatMemories(skills));
        Assert.Equal(("culpa_1", true), (guilt.Skill.Id, guilt.Guilt));

        Assert.Null(mosaic.StageOf("pesada_2"));
        Assert.Equal("pesada_2", Assert.Single(mosaic.LeftBehind).Id);
        Assert.Equal(new Lightness(20f, 1), heavy.Lightness);
    }

    [Fact]
    public void Data_ReadsTheHeavyPart()
    {
        var recollection = RecollectionData.FromJson("""
            {
              "id": "o_convite", "kind": "family", "scope": "heavy", "order": 1, "title_key": "t", "skill": "culpa_convite",
              "gate": { "flag": "chegou" },
              "warning_key": "a", "surface_key": "b",
              "stages": { "full": { "text_key": "v" } },
              "leave": { "text_key": "d", "lucidity_max": { "_tbd": true, "value": 10 }, "protections": 1 }
            }
            """);

        Assert.Equal(new HeavyMemory(1, new FlagSet("chegou"), "a", "b", "d", new Lightness(10f, 1)), recollection.Heavy);
        Assert.Throws<FormatException>(() => RecollectionData.FromJson("""
            { "id": "x", "kind": "family", "scope": "heavy", "title_key": "t", "skill": "c", "warning_key": "a",
              "surface_key": "b", "stages": { "full": { "text_key": "v" } }, "leave": { "text_key": "d", "lucidity_max": 1, "protections": 1 } }
            """));
    }

    private Mosaic Mosaic(IEnumerable<Recollection> recollections) =>
        new(_state, _bus, recollections.ToDictionary(r => r.Id), MosaicTests.Rules);

    private static MemorySkill Skill(string id) => new(
        id, RecollectionKind.Family, id, id,
        new Combat.Gestures.StrokePattern(new[] { Combat.Gestures.StrokeDirection.Down, Combat.Gestures.StrokeDirection.Up }, 0.5f, 0.2f, 0.5f),
        30f, 2f, MemoryEffectKind.Strike, new GradeValues(40f, 20f, 5f));
}
