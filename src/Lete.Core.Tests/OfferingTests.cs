using Lete.Core.Combat;
using Lete.Core.Conditions;
using Lete.Core.Dialogue;
using Lete.Core.Events;
using Lete.Core.Recollections;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Tests;

public class OfferingTests
{
    public static readonly OfferingRules Rules = new(new[] { "mel", "vinho", "leite", "agua", "farinha", "sangue" }, Capacity: 5, BloodKind: "sangue");

    private readonly WorldState _state = new();
    private readonly EventBus _bus = new();
    private readonly List<object> _events = new();
    private readonly Offerings _offerings;
    private readonly Healing _healing;
    private int _tideStep;

    public OfferingTests()
    {
        _bus.AnyPublished += e => _events.Add(e);
        _ = new MarkTracker(_state, _bus, new MarkRules(4, 8, 4, OfferingMark: new Dictionary<string, int> { ["sangue"] = 2 }));
        _offerings = new Offerings(_state, _bus, Rules);
        _healing = new Healing(_state, _bus, new HealingRules(HearthBreath: 40f, HospitalityBreath: 20f), _offerings, () => _tideStep);
    }

    [Fact]
    public void Pick_StopsAtTheLimitPerKind()
    {
        Assert.Equal(3, _offerings.Pick("mel", 3));
        Assert.Equal(2, _offerings.Pick("mel", 4));
        Assert.Equal(0, _offerings.Pick("mel", 1));
        Assert.Equal(5, _offerings.Count("mel"));
        Assert.Throws<ArgumentException>(() => _offerings.Pick("cerveja", 1));
    }

    [Fact]
    public void GiveAndShare_SpendAndPublish_BloodRaisesTheMark()
    {
        _offerings.Pick("farinha", 2);
        _offerings.Pick("sangue", 1);

        Assert.True(_offerings.Give("padeiro", "farinha"));
        Assert.True(_offerings.Share("padeiro", "farinha"));
        Assert.False(_offerings.Give("padeiro", "farinha"));
        Assert.Contains(new OfferingGiven("padeiro", "farinha"), _events);
        Assert.Contains(new OfferingShared("padeiro", "farinha"), _events);
        Assert.Equal(0, _state.Mark);

        _offerings.Give("remador", "sangue");
        Assert.Equal(2, _state.Mark);
    }

    [Fact]
    public void TriviaRestsEverything()
    {
        _state.Vitals.BreathSpent = 50f;
        _state.Vitals.LuciditySpent = 30f;
        _healing.Rest();

        Assert.Equal((0f, 0f), (_state.Vitals.BreathSpent, _state.Vitals.LuciditySpent));
        Assert.Contains(new Rested(), _events);
    }

    [Fact]
    public void Hearth_NeedsAnOffering_HealsOncePerTideStepPerFire()
    {
        _state.Vitals.BreathSpent = 70f;
        Assert.False(_healing.LightHearth("fogo_a"));

        _offerings.Pick("mel", 3);
        Assert.True(_healing.LightHearth("fogo_a"));
        Assert.Equal(30f, _state.Vitals.BreathSpent);
        Assert.Equal(2, _offerings.Count("mel"));
        Assert.False(_healing.LightHearth("fogo_a"));
        Assert.True(_healing.LightHearth("fogo_b"));
        Assert.Equal(0f, _state.Vitals.BreathSpent);

        _tideStep = 1;
        Assert.True(_healing.CanUse("fogo_a"));
        Assert.True(_healing.Hospitality("padeiro"));
        Assert.False(_healing.Hospitality("padeiro"));
    }

    [Fact]
    public void BattleBag_SpendsFromTheSavedBag()
    {
        _offerings.Pick("agua", 2);
        _state.Inventory.Provisions["pao"] = 1;
        var bag = new BattleBag(_state.Inventory, new[] { new ProvisionDefinition("pao", "n", "d", 30f, 0) });

        bag.SpendOffering("agua");
        bag.UseProvision("pao");

        Assert.Equal(1, _offerings.Count("agua"));
        Assert.Equal(0, _state.Inventory.Provisions["pao"]);
    }

    [Fact]
    public void InAConversation_TheRightOfferingAwakens_SharingAppliesItsEffects_BloodAwakensAnyone()
    {
        var applied = new List<DialogueEffect>();
        var graph = DialogueData.FromJson("""
            {
              "id": "padeiro", "mode": "shade", "speaker_key": "n", "shade": "padeiro",
              "lucidity": { "base": "vaga", "offering": "farinha", "share_effects": [ { "journal_key": "d.partilha" } ] },
              "start": [ "a" ], "nodes": [ { "id": "a", "line_key": "l", "effects": [ { "hospitality": true } ] } ]
            }
            """);
        var mosaic = new Mosaic(_state, _bus, new Dictionary<string, Recollection>(), MosaicTests.Rules);
        var facts = new WorldFacts(_state, new MarkRules(4, 8, 4), mosaic, new Profile(_state, _bus, ProfileTests.Rules));
        var dialogues = new Dialogues(_state, _bus, new Dictionary<string, DialogueGraph> { ["padeiro"] = graph }, facts,
            new Recorder(applied), _ => null, _offerings);
        _offerings.Pick("mel", 1);
        _offerings.Pick("farinha", 1);
        _offerings.Pick("sangue", 1);

        Assert.False(dialogues.Offer("padeiro", "mel", share: false));
        Assert.Equal(0, _offerings.Count("mel"));
        Assert.DoesNotContain("padeiro", _state.Dialogue.Awakened);

        Assert.True(dialogues.Offer("padeiro", "farinha", share: true));
        Assert.Contains("padeiro", _state.Dialogue.Awakened);
        Assert.Contains(new AddJournalEntry("d.partilha"), applied);
        Assert.Equal(ShadeLucidity.Awake, dialogues.Open("padeiro").Lucidity);
        Assert.Contains(new OfferHospitality("padeiro"), applied);

        Assert.True(dialogues.Offer("padeiro", "sangue", share: false));
    }

    private sealed class Recorder : IDialogueEffects
    {
        private readonly List<DialogueEffect> _applied;
        public Recorder(List<DialogueEffect> applied) => _applied = applied;
        public void Apply(DialogueEffect effect) => _applied.Add(effect);
    }
}
