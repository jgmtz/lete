using Lete.Core.Combat;
using Lete.Core.Conditions;
using Lete.Core.Events;
using Lete.Core.Items;
using Lete.Core.Recollections;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Tests;

public class ItemTests
{
    private readonly WorldState _state = new();
    private readonly EventBus _bus = new();
    private readonly List<object> _events = new();
    private readonly Journal _journal;
    private readonly Mosaic _mosaic;
    private readonly ItemTracker _items;

    public ItemTests()
    {
        _bus.AnyPublished += e => _events.Add(e);
        _journal = new Journal(_state, _bus);
        var barro = new Recollection("maos_no_barro", RecollectionKind.Craft, RecollectionScope.Common, null, "t",
            new Dictionary<RecollectionStage, string>(), null,
            new[] { new RecollectionSource("a", SourceType.Anchor, null, null, true, false), new RecollectionSource("b", SourceType.Dialogue, null, null, true, false) },
            new[] { "ato" }, 0, 0);
        _mosaic = new Mosaic(_state, _bus, new Dictionary<string, Recollection> { [barro.Id] = barro }, MosaicTests.Rules);
        var facts = new WorldFacts(_state, new MarkRules(4, 8, 4), _mosaic, new Profile(_state, _bus, ProfileTests.Rules));
        var items = new[]
        {
            Make("remo", ItemCategory.Instrument, "afogado",
                Stage(ItemStage.Worn, null, new ItemModifiers(0.05f, 0f, 0f, 0f)),
                Stage(ItemStage.Recognized, new AllOf(new Condition[] { new RecollectionKindFull(RecollectionKind.Craft), new FlagSet("missao_remo") }),
                    new ItemModifiers(0.1f, 0f, 0f, 0f), "remo.reconhecido")),
            Make("manto", ItemCategory.Garment, "afogado", Stage(ItemStage.Worn, null, new ItemModifiers(0f, 0.2f, 0f, 0f))),
            Make("moeda", ItemCategory.Votive, "afogado", Stage(ItemStage.Worn, null, new ItemModifiers(0f, 0f, 0f, 5f))),
            Make("figa", ItemCategory.Votive, null, Stage(ItemStage.Worn, null, ItemModifiers.None)),
            Make("lira", ItemCategory.Instrument, null, Stage(ItemStage.Worn, null, ItemModifiers.None),
                Stage(ItemStage.Recognized, new QuestStageMin("lira", 1), ItemModifiers.None)),
            Make("ramo", ItemCategory.Relic, null, Stage(ItemStage.Worn, null, new ItemModifiers(0f, 0f, -5f, 5f))),
        }.ToDictionary(i => i.Id);
        var sets = new Dictionary<string, ItemSet>
        {
            ["afogado"] = new("afogado", "n", "afogado.historia", new[] { "remo", "manto", "moeda" }, new ItemModifiers(0f, 0f, 10f, 0f)),
        };
        _items = new ItemTracker(_state, _bus, items, sets, () => facts);
    }

    private static ItemStageDefinition Stage(ItemStage stage, Condition? when, ItemModifiers modifiers, string? story = null) =>
        new(stage, when, story, modifiers);

    private static Item Make(string id, ItemCategory category, string? set, params ItemStageDefinition[] stages) =>
        new(id, category, $"{id}.nome", $"{id}.historia", RecollectionKind.Craft, set, stages, null);

    [Fact]
    public void Equip_ThreeSlotsAndTwoVotives_RelicsStayInTheSatchel()
    {
        foreach (var id in new[] { "remo", "manto", "moeda", "figa", "ramo" })
        {
            _items.Acquire(id);
        }

        _items.Equip("remo");
        _items.Equip("manto");
        _items.Equip("moeda");
        _items.Equip("figa");
        Assert.Equal("moeda", _items.EquippedIn(EquipSlot.Votive1));
        Assert.Equal("figa", _items.EquippedIn(EquipSlot.Votive2));
        Assert.Throws<InvalidOperationException>(() => _items.Equip("ramo"));
        Assert.Throws<InvalidOperationException>(() => _items.Equip("lira"));

        _items.Equip("figa", EquipSlot.Votive1);
        Assert.Equal("figa", _items.EquippedIn(EquipSlot.Votive1));
        Assert.Null(_items.EquippedIn(EquipSlot.Votive2));
        _items.Unequip("remo");
        Assert.Null(_items.EquippedIn(EquipSlot.Instrument));
    }

    [Fact]
    public void Modifiers_SumEquippedCompleteSetsAndRelics()
    {
        _items.Acquire("remo");
        _items.Acquire("manto");
        Assert.Equal(ItemModifiers.None, _items.Modifiers);

        _items.Equip("remo");
        _items.Equip("manto");
        _items.Acquire("ramo");
        Assert.Equal(new ItemModifiers(0.05f, 0.2f, -5f, 5f), _items.Modifiers);

        _items.Acquire("moeda");
        Assert.True(_items.IsSetComplete("afogado"));
        Assert.Contains(new ItemSetCompleted("afogado", "afogado.historia"), _events);
        Assert.Contains("afogado.historia", _journal.Entries);
        Assert.Equal(new ItemModifiers(0.05f, 0.2f, 5f, 5f), _items.Modifiers);
    }

    [Fact]
    public void Evolves_ByItself_WhenTheFullMemoryOfItsKindAndTheMissionArrive()
    {
        _items.Acquire("remo");
        _state.Flags.Add("missao_remo");
        _bus.Publish(new RegionEntered("x"));
        Assert.Equal(ItemStage.Worn, _items.StageOf("remo"));

        _mosaic.Hear("maos_no_barro", "a");
        _mosaic.Hear("maos_no_barro", "b");
        _mosaic.CompleteAct("maos_no_barro", "ato");

        Assert.Equal(ItemStage.Recognized, _items.StageOf("remo"));
        Assert.Contains(new ItemEvolved("remo", ItemStage.Recognized, "remo.reconhecido"), _events);
        Assert.Contains("remo.reconhecido", _journal.Entries);
    }

    [Fact]
    public void AStageOfAMissingSystem_Waits()
    {
        _items.Acquire("lira");
        _items.Refresh();

        Assert.Equal(ItemStage.Worn, _items.StageOf("lira"));
        Assert.Contains("lira.Recognized", _items.ConditionsWaitingForSystems);
    }

    [Fact]
    public void ItemOwnedCondition_ReadsTheItems()
    {
        var facts = new WorldFacts(_state, new MarkRules(4, 8, 4), _mosaic, new Profile(_state, _bus, ProfileTests.Rules));
        Assert.False(facts.OwnsItem("ramo"));
        _items.Acquire("ramo");
        Assert.True(facts.OwnsItem("ramo"));
        Assert.False(_items.Acquire("ramo"));
    }

    [Fact]
    public void Data_ReadsTheItemAndRejectsBrokenStages()
    {
        var item = ItemData.FromJson("""
            {
              "id": "remo", "category": "instrument", "name_key": "n", "story_key": "h", "affinity": "craft", "set": "afogado",
              "stages": [
                { "id": "worn", "modifiers": { "parry_window": { "_tbd": true, "value": 0.1 } } },
                { "id": "recognized", "when": { "recollection_kind_full": "craft" }, "story_key": "r", "modifiers": { "dodge_box": 0.2 } }
              ]
            }
            """);
        Assert.Equal((ItemCategory.Instrument, "afogado", 2), (item.Category, item.SetId, item.Stages.Count));
        Assert.Equal(new ItemModifiers(0.1f, 0f, 0f, 0f), item.Stages[0].Modifiers);
        Assert.Equal(new RecollectionKindFull(RecollectionKind.Craft), item.Stages[1].When);

        const string start = """{ "id": "x", "category": "instrument", "name_key": "n", "story_key": "h", "affinity": "craft", """;
        Assert.Throws<FormatException>(() => ItemData.FromJson(start + """ "stages": [ { "id": "recognized" } ] }"""));
        Assert.Throws<FormatException>(() => ItemData.FromJson(start + """ "stages": [ { "id": "worn" }, { "id": "awakened" } ] }"""));
        Assert.Throws<FormatException>(() => ItemData.FromJson(start + """ "stages": [ { "id": "worn", "modifiers": { "voar": 1 } } ] }"""));
        Assert.Throws<FormatException>(() => ItemData.FromJson(start + """ "unlocks_path": "aqueronte", "stages": [ { "id": "worn" } ] }"""));
        Assert.False(ItemData.IsItem("""{ "id": "bolsa_teste", "obols": 3 }"""));
    }
}
