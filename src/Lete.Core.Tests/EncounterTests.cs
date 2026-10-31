using Lete.Core.Combat;
using Lete.Core.Events;
using Lete.Core.State;
using Lete.Core.Systems;
using Lete.Core.World;

namespace Lete.Core.Tests;

/// <summary>Os encontros do mapa (o Remador-tutorial): lição, ordem, oferenda pedida e o arquivo.</summary>
public class EncounterTests
{
    private readonly WorldState _state = new();
    private readonly EventBus _bus = new();
    private readonly List<object> _events = new();
    private readonly Encounters _encounters;

    private static Encounter Make(string id, string? after = null, string? needs = null) => new(
        id, "remador", "tenaro", "remador_tenaro", new[] { "atacar" }, new[] { "atacar", "aparar" }, "h", "recua", after,
        needs, needs is null ? null : "espera", FreeDefeat: true);

    public EncounterTests()
    {
        _bus.AnyPublished += e => _events.Add(e);
        _encounters = new Encounters(_state, _bus, new Dictionary<string, Encounter>
        {
            ["boca"] = Make("boca"),
            ["poca"] = Make("poca", after: "boca"),
            ["marcas"] = Make("marcas", after: "poca", needs: "agua"),
        });
    }

    [Fact]
    public void Lesson_CompletesWhenEveryStepIsDone_InAnyOrder_IgnoringTheRest()
    {
        var lesson = new EncounterLesson(new[] { BattleActions.Attack, BattleActions.Parry });
        Assert.False(lesson.Record(BattleActions.Listen));
        Assert.True(lesson.Record(BattleActions.Parry));
        Assert.False(lesson.Record(BattleActions.Parry));
        Assert.False(lesson.IsComplete);
        Assert.True(lesson.Record(BattleActions.Attack));
        Assert.True(lesson.IsComplete);
    }

    [Fact]
    public void EachEncounter_WaitsForThePreviousOne_AndFinishesOnce()
    {
        Assert.True(_encounters.IsAvailable("boca"));
        Assert.False(_encounters.IsAvailable("poca"));
        Assert.False(_encounters.Finish("poca"));

        Assert.True(_encounters.Finish("boca"));
        Assert.False(_encounters.Finish("boca"));
        Assert.False(_encounters.IsAvailable("boca"));
        Assert.True(_encounters.IsAvailable("poca"));
        Assert.Equal(new object[] { new EncounterFinished("boca") }, _events);
        Assert.False(_encounters.IsAvailable("nao_existe"));
    }

    [Fact]
    public void AnEncounterThatNeedsAnOffering_StartsOnlyWithItInTheBag()
    {
        _encounters.Finish("boca");
        _encounters.Finish("poca");
        Assert.True(_encounters.IsAvailable("marcas"));
        Assert.False(_encounters.CanStart("marcas"));

        _state.Inventory.Offerings["agua"] = 1;
        Assert.True(_encounters.CanStart("marcas"));
        Assert.False(_encounters.CanStart("boca"));
    }

    [Fact]
    public void Allows_EveryActionWhenTheListIsMissing()
    {
        var full = Make("camara") with { Actions = null, Lesson = Array.Empty<string>() };
        Assert.False(full.IsLesson);
        Assert.True(full.Allows(BattleActions.Flee));
        Assert.False(Make("boca").Allows(BattleActions.Flee));
    }

    private const string Valid = """
        {
          "id": "remador_marcas", "shade": "remador", "region": "tenaro", "battle": "remador_tenaro",
          "after": "remador_poca",
          "actions": ["atacar", "aproximar", "escutar", "oferenda", "presenciar"],
          "lesson": ["oferenda", "presenciar", "aparar"],
          "hecate_key": "h", "withdraw_key": "r", "needs_offering": "agua", "wait_key": "w", "free_defeat": true
        }
        """;

    [Fact]
    public void FromJson_ReadsEveryField()
    {
        var encounter = WorldData.EncounterFromJson(Valid);
        Assert.Equal("remador_poca", encounter.After);
        Assert.Equal("agua", encounter.NeedsOffering);
        Assert.True(encounter.FreeDefeat);
        Assert.Equal(new[] { "oferenda", "presenciar", "aparar" }, encounter.Lesson);
        Assert.False(encounter.Allows(BattleActions.Flee));

        var full = WorldData.EncounterFromJson("""
            { "id": "c", "shade": "remador", "region": "tenaro", "battle": "b", "hecate_key": "h" }
            """);
        Assert.Null(full.Actions);
        Assert.False(full.IsLesson || full.FreeDefeat);
    }

    [Theory]
    [InlineData("\"atacar\", \"aproximar\"", "\"atacar\", \"voar\"")] // ação que não existe
    [InlineData("\"atacar\", \"aproximar\", \"escutar\"", "\"atacar\", \"escutar\"")] // escutar sem aproximar
    [InlineData("\"lesson\": [\"oferenda\", \"presenciar\", \"aparar\"]", "\"lesson\": [\"fugir\"]")] // lição fora das ações
    [InlineData("\"withdraw_key\": \"r\",", "")] // lição sem a fala do recuo
    [InlineData("\"wait_key\": \"w\",", "")] // oferenda sem a fala de espera
    [InlineData("\"after\": \"remador_poca\"", "\"after\": \"remador_marcas\"")] // depois de si mesmo
    public void FromJson_RejectsInconsistentEncounters(string from, string to)
    {
        var json = Valid.Replace(from, to);
        Assert.NotEqual(Valid, json);
        Assert.Throws<FormatException>(() => WorldData.EncounterFromJson(json));
    }
}
