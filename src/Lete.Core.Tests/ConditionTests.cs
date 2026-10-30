using Lete.Core.Recollections;
using Lete.Core.Combat;
using Lete.Core.Conditions;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Tests;

public class ConditionTests
{
    /// <summary>Respostas prontas: só o que cada teste precisa.</summary>
    private sealed class Facts : IConditionFacts
    {
        public string? Origin { get; set; }
        public Gender Gender { get; set; }
        public string? Companion { get; set; }
        public string? Difficulty { get; set; }
        public MarkBand MarkBand { get; set; }
        public int Tide { get; set; }
        public int Bond { get; set; }
        public int FullRecollectionKinds { get; set; }
        public Via? TopVia { get; set; }
        public RecollectionKind? TopKind { get; set; }
        public string? ShadeLucidity { get; set; }
        public HashSet<string> Flags { get; } = new();
        public Dictionary<string, (RecollectionStage Stage, RecollectionCertainty Certainty)> Recollections { get; } = new();
        public Dictionary<string, HeavyMemoryState> Heavy { get; } = new();
        public Dictionary<string, int> Quests { get; } = new();

        public bool HasFlag(string flag) => Flags.Contains(flag);
        public RecollectionStage? RecollectionStageOf(string id) => Recollections.TryGetValue(id, out var r) ? r.Stage : null;
        public RecollectionCertainty? RecollectionCertaintyOf(string id) => Recollections.TryGetValue(id, out var r) ? r.Certainty : null;
        public HeavyMemoryState HeavyMemory(string id) => Heavy.GetValueOrDefault(id, HeavyMemoryState.Pending);
        public bool HasPathKey(string key) => false;
        public bool IsOathActive(string oath) => false;
        public bool IsOathBroken(string oath) => false;
        public bool WasOfferingShared(string shade, string? kind) => false;
        public bool ShadeIs(string shade, ShadeState state) => false;
        public bool OwnsItem(string item) => false;
        public bool IsTriviaLit(string trivia) => false;
        public bool WasRegionVisited(string region) => false;
        public int? QuestStage(string quest) => Quests.TryGetValue(quest, out var stage) ? stage : null;
    }

    private static ConditionResult Eval(string json, Facts facts) =>
        ConditionEvaluator.Evaluate(ConditionParser.Parse(json), facts);

    [Fact]
    public void DadosExample_PassesOnlyWithEverything()
    {
        const string json = """
            { "all": [
              { "origin": "pescador" },
              { "recollection": { "id": "a_agua_parada", "min_stage": "recalled" } },
              { "mark_band": { "min": "mid" } },
              { "flag": "conheceu_aulo" }
            ] }
            """;
        var facts = new Facts { Origin = "pescador", MarkBand = MarkBand.Mid };
        facts.Flags.Add("conheceu_aulo");
        facts.Recollections["a_agua_parada"] = (RecollectionStage.Fragment, RecollectionCertainty.Certain);
        Assert.Equal(ConditionOutcome.Fail, Eval(json, facts).Outcome);

        facts.Recollections["a_agua_parada"] = (RecollectionStage.Full, RecollectionCertainty.Certain);
        Assert.Equal(ConditionOutcome.Pass, Eval(json, facts).Outcome);
    }

    [Fact]
    public void Combinators_AnyAndNot()
    {
        var facts = new Facts { Origin = "veterano" };
        Assert.True(Eval("""{ "any": [ { "origin": "pescador" }, { "origin": "veterano" } ] }""", facts).Passed);
        Assert.False(Eval("""{ "not": { "origin": "veterano" } }""", facts).Passed);
    }

    [Fact]
    public void NearMiss_WhenOnlyOneSoftPredicateFails()
    {
        const string json = """
            { "all": [
              { "origin": "artesao" },
              { "recollection": { "id": "as_maos_de_barro", "min_stage": "fragment" }, "soft": true }
            ] }
            """;
        var facts = new Facts { Origin = "artesao" };
        var result = Eval(json, facts);

        Assert.Equal(ConditionOutcome.NearMiss, result.Outcome);
        Assert.Equal("recollection", result.Missing!.Name);

        facts.Origin = "pescador";
        Assert.Equal(ConditionOutcome.Fail, Eval(json, facts).Outcome);
    }

    [Fact]
    public void NearMiss_NotWhenTwoSoftPredicatesFail()
    {
        const string json = """{ "all": [ { "flag": "a", "soft": true }, { "flag": "b", "soft": true } ] }""";
        var facts = new Facts();
        Assert.Equal(ConditionOutcome.Fail, Eval(json, facts).Outcome);

        facts.Flags.Add("a");
        Assert.Equal(ConditionOutcome.NearMiss, Eval(json, facts).Outcome);
    }

    [Fact]
    public void Predicates_MarkBandRangeHeavyMemoryAndQuest()
    {
        var facts = new Facts { MarkBand = MarkBand.High };
        Assert.False(Eval("""{ "mark_band": { "max": "mid" } }""", facts).Passed);
        Assert.True(Eval("""{ "mark_band": { "min": "mid", "max": "high" } }""", facts).Passed);

        Assert.True(Eval("""{ "heavy_memory": { "id": "o_convite", "state": "pending" } }""", facts).Passed);
        facts.Heavy["o_convite"] = HeavyMemoryState.Face;
        Assert.True(Eval("""{ "heavy_memory": { "id": "o_convite", "state": "face" } }""", facts).Passed);

        Assert.False(Eval("""{ "quest_stage": { "id": "cadeia_pescador_1", "min": 0 } }""", facts).Passed);
        facts.Quests["cadeia_pescador_1"] = 2;
        Assert.True(Eval("""{ "quest_stage": { "id": "cadeia_pescador_1", "min": 2 } }""", facts).Passed);
    }

    [Fact]
    public void Recollection_StateMustMatchWhenGiven()
    {
        var facts = new Facts();
        facts.Recollections["a"] = (RecollectionStage.Recalled, RecollectionCertainty.Uncertain);
        Assert.True(Eval("""{ "recollection": { "id": "a", "state": "uncertain" } }""", facts).Passed);
        Assert.False(Eval("""{ "recollection": { "id": "a", "state": "certain" } }""", facts).Passed);
    }

    [Fact]
    public void Profile_ReadsTopViaAndKind()
    {
        var facts = new Facts();
        Assert.False(Eval("""{ "profile": { "top_via": "free" } }""", facts).Passed);
        facts.TopVia = Via.Free;
        facts.TopKind = RecollectionKind.Mourning;
        Assert.True(Eval("""{ "profile": { "top_via": "free", "top_kind": "mourning" } }""", facts).Passed);
        Assert.Throws<ConditionException>(() => ConditionParser.Parse("""{ "profile": { "top_via": "libertar" } }"""));
    }

    [Theory]
    [InlineData("""{ "voar": true }""", "condition.voar")]
    [InlineData("""{ "origin": "a", "flag": "b" }""", "condition")]
    [InlineData("""{ "all": [] }""", "condition.all")]
    [InlineData("""{ "mark_band": { "min": "altissima" } }""", "condition.mark_band.min")]
    [InlineData("""{ "mark_band": { "min": "high", "max": "low" } }""", "condition.mark_band")]
    [InlineData("""{ "all": [ { "flag": "x" }, { "heavy_memory": { "id": "y" } } ] }""", "condition.all[1].heavy_memory")]
    [InlineData("""{ "not": { "flag": "x", "soft": true } }""", "condition.not")]
    [InlineData("""{ "tide_min": -1 }""", "condition.tide_min")]
    public void Parse_RejectsWithFieldPath(string json, string path)
    {
        var error = Assert.Throws<ConditionException>(() => ConditionParser.Parse(json));
        Assert.Equal(path, error.Path);
    }

    [Fact]
    public void Parse_KnowsEveryPredicateOfTheList()
    {
        // Um exemplo válido de cada predicado de DADOS.md 1: a lista do parser e a do documento não se separam.
        var examples = new Dictionary<string, string>
        {
            ["origin"] = "\"pescador\"",
            ["gender"] = "\"f\"",
            ["recollection"] = """{ "id": "a", "min_stage": "full", "state": "false" }""",
            ["recollection_kind_full_count"] = """{ "min": 2 }""",
            ["heavy_memory"] = """{ "id": "a", "state": "leave" }""",
            ["mark_band"] = """{ "min": "low" }""",
            ["tide_min"] = "2",
            ["flag"] = "\"a\"",
            ["path_key"] = "\"a\"",
            ["oath_active"] = "\"a\"",
            ["oath_broken"] = "\"a\"",
            ["offering_shared"] = """{ "shade": "a", "kind": "mel" }""",
            ["shade_state"] = """{ "id": "a", "state": "spared" }""",
            ["companion"] = "\"aulo\"",
            ["bond_min"] = "1",
            ["difficulty"] = "\"estige\"",
            ["profile"] = """{ "top_via": "free", "top_kind": "mourning" }""",
            ["item_owned"] = "\"a\"",
            ["trivia_lit"] = "\"a\"",
            ["region_visited"] = "\"a\"",
            ["quest_stage"] = """{ "id": "a", "min": 1 }""",
            ["shade_lucidity"] = "\"vaga\"",
        };

        Assert.Equal(ConditionParser.PredicateNames.OrderBy(n => n), examples.Keys.OrderBy(n => n));
        foreach (var (name, value) in examples)
        {
            var predicate = Assert.IsAssignableFrom<Predicate>(ConditionParser.Parse($$"""{ "{{name}}": {{value}} }"""));
            Assert.Equal(name, predicate.Name);
        }
    }

    [Fact]
    public void WorldFacts_AnswersFromStateAndNamesMissingSystems()
    {
        var state = new WorldState { OriginId = "veterano", Mark = 5 };
        state.Flags.Add("viu_o_rio");
        state.HeavyMemories["o_convite"] = HeavyMemoryChoice.Leave;
        var mosaic = new Mosaic(state, new Events.EventBus(), new Dictionary<string, Recollection>(), MosaicTests.Rules);
        var facts = new WorldFacts(state, new MarkRules(MidFrom: 4, HighFrom: 8, FacedToReturnFace: 4), mosaic,
            new Profile(state, new Events.EventBus(), ProfileTests.Rules));

        var condition = ConditionParser.Parse("""
            { "all": [
              { "origin": "veterano" }, { "flag": "viu_o_rio" }, { "mark_band": { "min": "mid", "max": "mid" } },
              { "heavy_memory": { "id": "o_convite", "state": "leave" } }
            ] }
            """);
        Assert.True(ConditionEvaluator.Evaluate(condition, facts).Passed);

        var error = Assert.Throws<NotSupportedException>(() =>
            ConditionEvaluator.Evaluate(ConditionParser.Parse("""{ "bond_min": 1 }"""), facts));
        Assert.Contains("vínculo", error.Message);
    }
}
