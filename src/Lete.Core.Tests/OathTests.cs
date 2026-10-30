using Lete.Core.Combat;
using Lete.Core.Conditions;
using Lete.Core.Data;
using Lete.Core.Events;
using Lete.Core.Recollections;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Tests;

public class OathTests
{
    private readonly WorldState _state = new();
    private readonly EventBus _bus = new();
    private readonly List<object> _events = new();
    private readonly MarkTracker _mark;
    private readonly Oaths _oaths;

    public OathTests()
    {
        _bus.AnyPublished += e => _events.Add(e);
        var markRules = new MarkRules(4, 8, 4, PerjuryMark: 2);
        _mark = new MarkTracker(_state, _bus, markRules);
        var mosaic = new Mosaic(_state, _bus, new Dictionary<string, Recollection>(), MosaicTests.Rules);
        var facts = new WorldFacts(_state, markRules, mosaic, new Profile(_state, _bus, ProfileTests.Rules));
        var oaths = new[]
        {
            Make("mao_aberta", new[] { Via.Absorb }, blessing: new OathEffects(10f, false), heel: new OathEffects(0f, true)),
            Make("do_lar", Array.Empty<Via>(), kept: new FlagSet("voltou"), expires: new FlagSet("tartaro")),
            Make("do_silencio", Array.Empty<Via>()),
            Make("da_noite", new[] { Via.Flee }),
            Make("do_campo", Array.Empty<Via>(), kept: new RegionVisited("lete")),
        }.ToDictionary(o => o.Id);
        _oaths = new Oaths(_state, _bus, oaths, new OathRules(MaxActive: 3), () => facts);
    }

    private static Oath Make(
        string id, Via[] breaksOn, Condition? kept = null, Condition? expires = null, OathEffects? blessing = null, OathEffects? heel = null) =>
        new(id, $"{id}.nome", $"{id}.promessa", $"{id}.bencao", $"{id}.calcanhar", breaksOn, kept, expires,
            blessing ?? OathEffects.None, heel ?? OathEffects.None);

    [Fact]
    public void AtMostThreeActive_EachSwornOnce()
    {
        Assert.True(_oaths.Swear("mao_aberta"));
        Assert.False(_oaths.Swear("mao_aberta"));
        Assert.True(_oaths.Swear("do_lar"));
        Assert.True(_oaths.Swear("do_silencio"));
        Assert.False(_oaths.CanSwear("da_noite"));
        Assert.Contains(new OathSworn("do_lar"), _events);
    }

    [Fact]
    public void TheForbiddenVia_IsPerjury_MarkRises_BlessingGone()
    {
        _oaths.Swear("mao_aberta");
        Assert.Equal(new OathEffects(10f, true), _oaths.Effects);
        Assert.Single(_oaths.WouldBreak(Via.Absorb));
        Assert.Empty(_oaths.WouldBreak(Via.Free));

        _bus.Publish(new ShadeAbsorbed("remador"));

        Assert.Equal(OathStatus.Broken, _oaths.StatusOf("mao_aberta"));
        Assert.Contains(new OathBroken("mao_aberta"), _events);
        Assert.Equal(2, _state.Mark);
        Assert.Equal(OathEffects.None, _oaths.Effects);
        Assert.False(_oaths.Swear("mao_aberta"));
    }

    [Fact]
    public void KeptBeforeTheDeadline_EndsAndFreesTheSlot()
    {
        _oaths.Swear("do_lar");
        _oaths.Swear("do_silencio");
        _oaths.Swear("mao_aberta");
        _state.Flags.Add("voltou");
        _bus.Publish(new RegionEntered("asfodelos"));

        Assert.Equal(OathStatus.Kept, _oaths.StatusOf("do_lar"));
        Assert.Contains(new OathKept("do_lar"), _events);
        Assert.True(_oaths.Swear("da_noite"));
        Assert.Equal(0, _state.Mark);
    }

    [Fact]
    public void TheDeadlinePassingFirst_IsPerjury()
    {
        _oaths.Swear("do_lar");
        _state.Flags.Add("tartaro");
        _oaths.Refresh();

        Assert.Equal(OathStatus.Broken, _oaths.StatusOf("do_lar"));
        Assert.Equal(2, _state.Mark);
    }

    [Fact]
    public void ASceneCanBreakIt_AndAConditionOfAMissingSystemWaits()
    {
        _oaths.Swear("do_silencio");
        Assert.True(_oaths.Break("do_silencio"));
        Assert.False(_oaths.Break("do_silencio"));

        _oaths.Swear("do_campo");
        Assert.Equal(OathStatus.Active, _oaths.StatusOf("do_campo"));
        Assert.Contains("do_campo.kept_when", _oaths.ConditionsWaitingForSystems);
    }

    [Fact]
    public void Conditions_ReadActiveAndBroken()
    {
        var facts = new WorldFacts(_state, new MarkRules(4, 8, 4), new Mosaic(_state, _bus, new Dictionary<string, Recollection>(), MosaicTests.Rules),
            new Profile(_state, _bus, ProfileTests.Rules));
        _oaths.Swear("mao_aberta");
        Assert.True(facts.IsOathActive("mao_aberta"));
        _oaths.Break("mao_aberta");
        Assert.False(facts.IsOathActive("mao_aberta"));
        Assert.True(facts.IsOathBroken("mao_aberta"));
    }

    [Fact]
    public void Data_ReadsTheOathAndRejectsUnknownViaOrEffect()
    {
        var oath = OathData.FromJson("""
            {
              "id": "mao_aberta", "name_key": "n", "promise_key": "p", "blessing_key": "b", "heel_key": "h",
              "breaks_on": [ "absorb" ], "kept_when": { "flag": "lete" },
              "blessing": { "lucidity_max": { "_tbd": true, "value": 10 } }, "heel": { "echoes_blocked": true }
            }
            """);
        Assert.Equal(new[] { Via.Absorb }, oath.BreaksOn);
        Assert.Equal(new OathEffects(10f, false), oath.Blessing);
        Assert.Equal(new OathEffects(0f, true), oath.Heel);
        Assert.Null(oath.ExpiresWhen);

        const string start = """{ "id": "x", "name_key": "n", "promise_key": "p", "blessing_key": "b", "heel_key": "h", """;
        Assert.Throws<FormatException>(() => OathData.FromJson(start + """ "breaks_on": [ "voar" ] }"""));
        Assert.Throws<FormatException>(() => OathData.FromJson(start + """ "blessing": { "voar": 1 } }"""));
    }
}
