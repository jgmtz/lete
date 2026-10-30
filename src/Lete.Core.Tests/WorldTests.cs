using Lete.Core.Combat;
using Lete.Core.Events;
using Lete.Core.Items;
using Lete.Core.Recollections;
using Lete.Core.State;
using Lete.Core.Systems;
using Lete.Core.World;

namespace Lete.Core.Tests;

public class WorldTests
{
    private readonly WorldState _state = new();
    private readonly EventBus _bus = new();
    private readonly List<object> _events = new();
    private readonly TideTracker _tide;
    private readonly Profile _profile;
    private readonly Offerings _offerings;
    private readonly ItemTracker _items;
    private readonly Mosaic _mosaic;

    public WorldTests()
    {
        _bus.AnyPublished += e => _events.Add(e);
        _ = new MarkTracker(_state, _bus, new MarkRules(4, 8, 4, PerjuryMark: 2));
        _tide = new TideTracker(_state, _bus, new TideRules(1m, 0.1m));
        _profile = new Profile(_state, _bus, ProfileTests.Rules);
        _offerings = new Offerings(_state, _bus, OfferingTests.Rules);
        var manto = new Item("manto", ItemCategory.Garment, "n", "h", RecollectionKind.Mourning, null,
            new[] { new ItemStageDefinition(ItemStage.Worn, null, null, ItemModifiers.None) }, null);
        var recollections = new[]
        {
            Recollection("maos", reliable: true),
            Recollection("agua", reliable: false, contradiction: true),
            Recollection("sonho", reliable: false),
        }.ToDictionary(r => r.Id);
        _mosaic = new Mosaic(_state, _bus, recollections, MosaicTests.Rules);
        var facts = new Conditions.WorldFacts(_state, new MarkRules(4, 8, 4), _mosaic, _profile);
        _items = new ItemTracker(_state, _bus, new Dictionary<string, Item> { ["manto"] = manto }, new Dictionary<string, ItemSet>(), () => facts);
    }

    private static Recollection Recollection(string id, bool reliable, bool contradiction = false) =>
        new(id, RecollectionKind.Craft, RecollectionScope.Common, null, "t", new Dictionary<RecollectionStage, string>(), null,
            contradiction
                ? new[] { new RecollectionSource("a", SourceType.Dream, null, null, reliable, false), new RecollectionSource("contra", SourceType.Dialogue, null, null, true, true) }
                : new[] { new RecollectionSource("a", SourceType.Dream, null, null, reliable, false) },
            Array.Empty<string>(), 0, 0);

    // ---- Mercadores ----

    private Merchants NewMerchants() => new(_state, _bus, new Dictionary<string, Merchant>
    {
        ["coveiro"] = new("coveiro", "n", "s", new[]
        {
            new Ware(WareKind.Offering, "mel", 2, 2, Array.Empty<string>()),
            new Ware(WareKind.Item, "manto", 8, 1, new[] { "fight" }),
        }),
    }, new MerchantRules(1.5f), _profile, _offerings, _items, () => _tide.Step);

    [Fact]
    public void Merchant_OffPathCostsMore_UntilTheProfileMatches()
    {
        var merchants = NewMerchants();
        var manto = merchants.Offers("coveiro").Single(o => o.Ware.Id == "manto");
        Assert.Equal((12, false), (manto.Price, manto.OnPath));
        Assert.Equal(2, merchants.Offers("coveiro").Single(o => o.Ware.Id == "mel").Price);

        foreach (var shade in new[] { "a", "b" })
        {
            _bus.Publish(new ShadeDestroyed(shade));
        }

        manto = merchants.Offers("coveiro").Single(o => o.Ware.Id == "manto");
        Assert.Equal((8, true), (manto.Price, manto.OnPath));
    }

    [Fact]
    public void Merchant_Buys_RunsOut_AndRestocksOnTheNextTideStep()
    {
        var merchants = NewMerchants();
        var mel = merchants.Offers("coveiro").Single(o => o.Ware.Id == "mel").Ware;
        Assert.Equal(PurchaseResult.NotEnoughObols, merchants.Buy("coveiro", mel));

        _state.Inventory.Obols = 20;
        Assert.Equal(PurchaseResult.Bought, merchants.Buy("coveiro", mel));
        Assert.Equal(PurchaseResult.Bought, merchants.Buy("coveiro", mel));
        Assert.Equal(PurchaseResult.SoldOut, merchants.Buy("coveiro", mel));
        Assert.Equal((16, 2), (_state.Inventory.Obols, _offerings.Count("mel")));
        Assert.Contains(new WareBought("coveiro", "oferenda:mel", 2), _events);

        _tide.Milestone("marco");
        Assert.Equal(2, merchants.Offers("coveiro").Single(o => o.Ware.Id == "mel").Remaining);
    }

    [Fact]
    public void Merchant_SellsTheUniqueItemOnce_AndRespectsTheBagLimit()
    {
        var merchants = NewMerchants();
        _state.Inventory.Obols = 30;
        var manto = merchants.Offers("coveiro").Single(o => o.Ware.Id == "manto").Ware;
        Assert.Equal(PurchaseResult.Bought, merchants.Buy("coveiro", manto));
        Assert.True(_items.Owns("manto"));
        Assert.DoesNotContain(merchants.Offers("coveiro"), o => o.Ware.Id == "manto");

        _offerings.Pick("mel", 5);
        var mel = merchants.Offers("coveiro").Single(o => o.Ware.Id == "mel").Ware;
        Assert.Equal(PurchaseResult.BagFull, merchants.Buy("coveiro", mel));
        Assert.Equal(18, _state.Inventory.Obols);
    }

    // ---- Trívias ----

    private Trivias NewTrivias() => new(_state, _bus, new Dictionary<string, Trivia>
    {
        ["asfodelos"] = new("asfodelos", "asfodelos", "n", Array.Empty<string>(), Hub: true),
        ["luto"] = new("luto", "campos_do_luto", "n", new[] { "asfodelos", "tribunal" }, Hub: false),
        ["tribunal"] = new("tribunal", "tribunal", "n", new[] { "luto" }, Hub: false),
        ["guerreiros"] = new("guerreiros", "guerreiros", "n", new[] { "asfodelos" }, Hub: false),
    }, _tide);

    [Fact]
    public void Trivias_TravelOnlyByLitTorches_TheHubReachesAllLit_TideRises()
    {
        var trivias = NewTrivias();
        Assert.Empty(trivias.Destinations("luto"));

        trivias.Light("luto");
        trivias.Light("asfodelos");
        trivias.Light("guerreiros");
        Assert.False(trivias.Light("luto"));
        Assert.Equal(new[] { "asfodelos" }, trivias.Destinations("luto").Select(t => t.Id));
        Assert.Equal(new[] { "guerreiros", "luto" }, trivias.Destinations("asfodelos").Select(t => t.Id));
        Assert.Throws<InvalidOperationException>(() => trivias.Travel("luto", "guerreiros"));

        trivias.Travel("luto", "asfodelos");
        Assert.Equal(0.1m, _state.Tide);
        Assert.Contains(new FastTravelUsed("luto", "asfodelos"), _events);
        Assert.Contains(new TriviaLit("guerreiros"), _events);
    }

    // ---- Tribunal ----

    private Tribunal NewTribunal() => new(_state, _bus, new Dictionary<string, Trial>
    {
        ["padeiro"] = new("padeiro", "minos", "padeiro", "intro", 2,
            new[]
            {
                new WeightedEvidence(new Evidence(EvidenceKind.Journal, "mulher"), 2),
                new WeightedEvidence(new Evidence(EvidenceKind.Journal, "fornada"), -1),
                new WeightedEvidence(new Evidence(EvidenceKind.Recollection, "maos"), 1),
            },
            new[] { "sonho" },
            new[]
            {
                new Verdict(ShadeDestination.Elysium, 3, "elisios"),
                new Verdict(ShadeDestination.Asphodel, 0, "asfodelos"),
                new Verdict(ShadeDestination.Tartarus, null, "tartaro"),
            }),
    }, new TribunalRules(DefendBonus: 1, AccuseBonus: -1), _mosaic);

    private void Remember()
    {
        _state.JournalEntries.AddRange(new[] { "mulher", "fornada" });
        _mosaic.Hear("maos", "a");
    }

    [Fact]
    public void Trial_ScoreIsEvidencePlusStance_ThenTheVerdict_OnlyOnce()
    {
        Remember();
        var tribunal = NewTribunal();
        var outcome = tribunal.Judge("padeiro", TrialStance.Defend,
            new[] { new Evidence(EvidenceKind.Journal, "mulher"), new Evidence(EvidenceKind.Recollection, "maos") });

        Assert.Equal((ShadeDestination.Elysium, 4), (outcome.Verdict.Destination, outcome.Score));
        Assert.Equal(ShadeDestination.Elysium, tribunal.VerdictOf("padeiro"));
        Assert.Contains(new TestimonyGiven("padeiro", "maos"), _events);
        Assert.Equal(JudgeLeaning.Clement, _profile.Judge);
        Assert.Throws<InvalidOperationException>(() => tribunal.Judge("padeiro", TrialStance.Silence, Array.Empty<Evidence>()));
    }

    [Fact]
    public void Trial_AccuseWithTheBadEvidence_GoesToTartarus_SilenceIgnoresEvidence()
    {
        Remember();
        var outcome = NewTribunal().Judge("padeiro", TrialStance.Accuse, new[] { new Evidence(EvidenceKind.Journal, "fornada") });
        Assert.Equal(ShadeDestination.Tartarus, outcome.Verdict.Destination);
        Assert.Equal(JudgeLeaning.Severe, _profile.Judge);

        var state = new WorldState();
        state.JournalEntries.Add("fornada");
        var silent = new Tribunal(state, _bus, NewTribunal().Definitions, new TribunalRules(1, -1),
            new Mosaic(state, _bus, new Dictionary<string, Recollection>(), MosaicTests.Rules))
            .Judge("padeiro", TrialStance.Silence, new[] { new Evidence(EvidenceKind.Journal, "fornada") });
        Assert.Equal((ShadeDestination.Asphodel, 0), (silent.Verdict.Destination, silent.Score));
    }

    [Fact]
    public void Trial_AFalseRecollectionIsAlwaysPerjury_AnUncertainOneOnlyWhenMarked()
    {
        _mosaic.Hear("agua", "a");
        _mosaic.Hear("agua", "contra");
        _mosaic.Hear("sonho", "a");
        Assert.Equal(RecollectionCertainty.False, _mosaic.CertaintyOf("agua"));

        var outcome = NewTribunal().Judge("padeiro", TrialStance.Defend,
            new[] { new Evidence(EvidenceKind.Recollection, "agua"), new Evidence(EvidenceKind.Recollection, "sonho") });

        Assert.Equal(new[] { "agua", "sonho" }, outcome.Perjuries);
        Assert.Equal(2, _state.Perjuries);
        Assert.Equal(4, _state.Mark);
        Assert.Contains(new TestimonyPerjury("padeiro", "agua"), _events);
    }

    [Fact]
    public void Trial_RejectsTooManyOrUnknownEvidence()
    {
        Remember();
        var tribunal = NewTribunal();
        Assert.Throws<ArgumentException>(() => tribunal.Judge("padeiro", TrialStance.Defend, new[] { new Evidence(EvidenceKind.Journal, "nunca_vi") }));
        Assert.Throws<ArgumentException>(() => tribunal.Judge("padeiro", TrialStance.Defend, new[]
        {
            new Evidence(EvidenceKind.Journal, "mulher"), new Evidence(EvidenceKind.Journal, "fornada"), new Evidence(EvidenceKind.Recollection, "maos"),
        }));
    }
}
