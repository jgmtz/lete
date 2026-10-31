using System.Collections;
using System.Reflection;
using Lete.Core.Combat;
using Lete.Core.Conditions;
using Lete.Core.Events;
using Lete.Core.Items;
using Lete.Core.State;
using Lete.Core.Systems;
using Lete.Core.World;

namespace Lete.Core.Tests;

/// <summary>O estado único do mundo: o que entrou nele no M2 e a garantia de que o save leva tudo.</summary>
public class WorldStateTests
{
    private readonly WorldState _state = new();
    private readonly EventBus _bus = new();
    private readonly List<object> _events = new();

    public WorldStateTests() => _bus.AnyPublished += e => _events.Add(e);

    private WorldFacts Facts() => new(_state, new MarkRules(4, 8, 4),
        new Recollections.Mosaic(_state, _bus, new Dictionary<string, Recollections.Recollection>(), MosaicTests.Rules),
        new Profile(_state, _bus, ProfileTests.Rules));

    private static bool Holds(string json, IConditionFacts facts) =>
        ConditionEvaluator.Evaluate(ConditionParser.Parse(json), facts).Passed;

    [Fact]
    public void ShadeRecord_KeepsTheLatestOutcome_ForShadeState()
    {
        using var shades = new ShadeRecord(_state, _bus);
        _bus.Publish(new ShadeSpared("remador"));
        _bus.Publish(new ShadeWitnessed("tecela"));
        Assert.Equal(ShadeState.Spared, shades.StateOf("remador"));

        _bus.Publish(new ShadeFreed("remador"));
        var facts = Facts();
        Assert.True(Holds("""{ "shade_state": { "id": "remador", "state": "freed" } }""", facts));
        Assert.False(Holds("""{ "shade_state": { "id": "remador", "state": "spared" } }""", facts));
        Assert.True(Holds("""{ "shade_state": { "id": "tecela", "state": "seen" } }""", facts));
    }

    [Fact]
    public void Regions_EnteringAnotherIsAChange_VisitedStays()
    {
        var regions = new Regions(_state, _bus);
        Assert.True(regions.Enter("asfodelos"));
        Assert.False(regions.Enter("asfodelos"));
        Assert.True(regions.Enter("tribunal"));
        Assert.True(regions.Enter("asfodelos"));
        Assert.Equal(new[] { new RegionEntered("asfodelos"), new RegionEntered("tribunal"), new RegionEntered("asfodelos") }, _events);
        Assert.True(Holds("""{ "region_visited": "tribunal" }""", Facts()));
        Assert.False(Holds("""{ "region_visited": "tartaro" }""", Facts()));
    }

    [Fact]
    public void Offerings_SharingIsRemembered_ForOfferingShared()
    {
        var offerings = new Offerings(_state, _bus, OfferingTests.Rules);
        offerings.Pick("mel", 2);
        offerings.Give("padeiro", "mel");
        Assert.False(Holds("""{ "offering_shared": { "shade": "padeiro" } }""", Facts()));

        offerings.Share("padeiro", "mel");
        Assert.True(Holds("""{ "offering_shared": { "shade": "padeiro" } }""", Facts()));
        Assert.True(Holds("""{ "offering_shared": { "shade": "padeiro", "kind": "mel" } }""", Facts()));
        Assert.False(Holds("""{ "offering_shared": { "shade": "padeiro", "kind": "vinho" } }""", Facts()));
    }

    [Fact]
    public void Difficulty_StartsAtEstige_AndAnswersTheCondition()
    {
        Assert.True(Holds("""{ "difficulty": "estige" }""", Facts()));
        _state.Difficulty = "tartaro";
        Assert.False(Holds("""{ "difficulty": "estige" }""", Facts()));
    }

    [Fact]
    public void Hoard_LivesInTheState_PerShade()
    {
        var inventory = new InventoryState { Obols = 5 };
        inventory.Offerings["mel"] = 1;
        var bag = new BattleBag(inventory, Array.Empty<ProvisionDefinition>());
        var stored = new HoardState();
        new ShadeHoard(stored).TakeFrom(bag);
        Assert.Equal((5, 1), (stored.Obols, stored.Offerings["mel"]));

        Assert.True(new ShadeHoard(stored).ReturnTo(bag));
        Assert.True(stored.IsEmpty);
        Assert.Equal(5, inventory.Obols);
    }

    // ---- O save leva tudo ----

    /// <summary>Um estado com todos os campos preenchidos (o teste abaixo confere que nenhum ficou de fora).</summary>
    private static WorldState Full()
    {
        var s = new WorldState
        {
            Gender = Gender.Feminine,
            ProtagonistId = "sombra_f",
            OriginId = "artesao",
            CompanionId = "lia",
            CompanionBond = 4,
            CompanionTalks = 2,
            CompanionTalkStep = 1,
            Mark = 6,
            MarkStage = 2,
            FinalFace = FinalFace.Covered,
            Tide = 1.3m,
            Perjuries = 1,
            RegionId = "sala_teste",
            Position = new System.Numerics.Vector3(1f, 0f, -2.5f),
            Difficulty = "tartaro",
        };
        s.TideMilestones.Add("aqueronte");
        s.Recollections["agua_parada"] = new RecollectionProgress { FullAct = "remo", False = true };
        s.Recollections["agua_parada"].Sources.Add("fonte_a");
        s.HeavyMemories["o_convite"] = HeavyMemoryChoice.Face;
        s.Profile.ViaTotals[Via.Free] = 3;
        s.Profile.KindTotals[RecollectionKind.Craft] = 2;
        s.Profile.TrialStances[TrialStance.Defend] = 1;
        s.Profile.Recent.Add(new ProfileEncounter(Via.Free, new[] { RecollectionKind.Craft }));
        s.Merchants["coveiro/oferenda:mel"] = new MerchantSale(2, 1);
        s.LitTrivias.Add("trivia_sala");
        s.Trials["julgamento_padeiro"] = ShadeDestination.Asphodel;
        s.Items.Owned["remo_do_afogado"] = (int)ItemStage.Recognized;
        s.Items.Equipped[EquipSlot.Instrument] = "remo_do_afogado";
        s.Items.CompletedSets.Add("conjunto_do_afogado");
        s.Inventory.Obols = 7;
        s.Inventory.Offerings["mel"] = 2;
        s.Inventory.Provisions["pao"] = 1;
        s.Vitals.BreathSpent = 12.5f;
        s.Vitals.LuciditySpent = 3f;
        s.Vitals.HealUses["fogo_sala"] = 1;
        s.Dialogue.Chosen.Add("padeiro/vago_inicio/0");
        s.Dialogue.CommentsSeen.Add("comentario_1");
        s.Dialogue.Awakened.Add("padeiro");
        s.Oaths["mao_aberta"] = OathStatus.Broken;
        s.PathKeys["aqueronte"] = "obolo";
        s.OriginFragments.Add("artesao_padeiro");
        s.Flags.Add("viu_o_rio");
        s.JournalEntries.Add("diario.algo");
        s.Shades["remador"] = ShadeState.Freed;
        s.SharedOfferings.Add("padeiro/farinha");
        s.Echoes.Add("eco_remador");
        s.Hoards["remador"] = new HoardState { Obols = 3 };
        s.Hoards["remador"].Offerings["vinho"] = 1;
        s.VisitedRegions.Add("sala_teste");
        s.Encounters.Add("remador_boca");
        return s;
    }

    [Fact]
    public void Save_FullState_EveryFieldIsFilled_AndSurvivesTheRoundTrip()
    {
        var full = Full();
        // Campo novo no WorldState sem preencher aqui (e, quase sempre, sem ir para o save) falha este teste.
        Assert.Empty(EmptyFields(full, "state"));

        var text = SaveGame.Write(full);
        var loaded = SaveGame.Read(text);
        Assert.Equal(text, SaveGame.Write(loaded));
        Assert.Empty(EmptyFields(loaded, "state"));
    }

    /// <summary>Propriedades públicas com o valor padrão (nulo, zero, vazio), descendo nos objetos do estado.</summary>
    private static IEnumerable<string> EmptyFields(object target, string path)
    {
        foreach (var property in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0))
        {
            var value = property.GetValue(target);
            var at = $"{path}.{property.Name}";
            var type = property.PropertyType;
            var isDefault = value switch
            {
                null => true,
                string text => text.Length == 0,
                IEnumerable items => !items.Cast<object>().Any(),
                bool => false,
                _ when type.IsValueType => value.Equals(Activator.CreateInstance(type)),
                _ => false,
            };
            if (isDefault)
            {
                yield return at;
            }
            else if (value is not null && type.Namespace == typeof(WorldState).Namespace && type.IsClass && value is not IEnumerable)
            {
                foreach (var inner in EmptyFields(value, at))
                {
                    yield return inner;
                }
            }
        }
    }
}
