using Lete.Core.Combat;
using Lete.Core.Dialogue;
using Lete.Core.Events;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Tests;

public class CompanionTests
{
    public static readonly CompanionRules Rules = new(BondMax: 10, FastBond: 6, IntervalTurns: 2, FastIntervalTurns: 1, ReluctantFactor: 0.5f,
        new Dictionary<BondSource, int>
        {
            [BondSource.OfferingShared] = 1,
            [BondSource.Battle] = 1,
            [BondSource.TriviaTalk] = 1,
            [BondSource.CommentAgreed] = 1,
            [BondSource.CommentDisagreed] = 2,
        });

    private readonly WorldState _state = new();
    private readonly EventBus _bus = new();
    private readonly List<object> _events = new();
    private readonly TideTracker _tide;
    private readonly Companions _companions;

    public CompanionTests()
    {
        _bus.AnyPublished += e => _events.Add(e);
        _tide = new TideTracker(_state, _bus, new TideRules(1m, 0.1m));
        _companions = new Companions(_state, _bus, new Dictionary<string, Companion>
        {
            ["aulo"] = Aulo,
            ["ismena"] = Aulo with { Id = "ismena", SupportId = "lamento", Effect = SupportEffect.WidenListen },
        }, Rules, _tide);
    }

    private static readonly Companion Aulo = new("aulo", "n", "postura", SupportEffect.WidenDefense, 0.2f, 0,
        new Dictionary<Via, Approval> { [Via.Fight] = Approval.Approve, [Via.Absorb] = Approval.Reprove },
        "relutante", new[] { "conversa.1", "conversa.2" });

    [Fact]
    public void Choose_OncePerRun_RaisesTheTide()
    {
        Assert.False(_companions.Choose("ninguem"));
        Assert.True(_companions.Choose("aulo"));
        Assert.False(_companions.Choose("ismena"));
        Assert.Equal("aulo", _companions.Current!.Id);
        Assert.Equal(1m, _state.Tide);
        Assert.Contains(new CompanionChosen("aulo"), _events);
    }

    [Fact]
    public void Approval_MissingViasAreTolerated()
    {
        Assert.Equal(Approval.Approve, _companions.ApprovalOf("aulo", Via.Fight));
        Assert.Equal(Approval.Reprove, _companions.ApprovalOf("aulo", Via.Absorb));
        Assert.Equal(Approval.Tolerate, _companions.ApprovalOf("aulo", Via.Spare));
    }

    [Fact]
    public void Bond_GrowsOnlyForTheChosenOne_FromSharingFightingAndComments()
    {
        _bus.Publish(new ShadeFreed("remador"));
        Assert.Equal(0, _companions.Bond);

        _companions.Choose("aulo");
        _bus.Publish(new OfferingShared("padeiro", "mel"));
        Assert.Equal(0, _companions.Bond);

        _bus.Publish(new OfferingShared("aulo", "mel"));
        _bus.Publish(new ShadeAbsorbed("remador"));
        _bus.Publish(new CompanionCommented("c1", "aulo", CommentResponse.Disagree));
        _bus.Publish(new CompanionCommented("c2", "aulo", CommentResponse.Ignore));
        _bus.Publish(new CompanionCommented("c3", "ismena", CommentResponse.Agree));
        Assert.Equal(4, _companions.Bond);
        Assert.Contains(new CompanionBondChanged("aulo", 4), _events);
    }

    [Fact]
    public void Bond_StopsAtTheMax_AndHighBondShortensTheSupport()
    {
        _companions.Choose("aulo");
        Assert.Equal(2, _companions.Support("aulo").IntervalTurns);
        for (var i = 0; i < 20; i++)
        {
            _bus.Publish(new ShadeDestroyed("s"));
        }

        Assert.Equal(10, _companions.Bond);
        var support = _companions.Support("aulo");
        Assert.Equal((1, 0.2f, 0.5f), (support.IntervalTurns, support.Power, support.ReluctantFactor));
    }

    [Fact]
    public void TriviaTalk_OncePerTideStep_InOrder_TheLastRepeats()
    {
        Assert.False(_companions.CanTalk);
        _companions.Choose("aulo");
        Assert.True(_companions.CanTalk);
        Assert.Equal("conversa.1", _companions.Talk());
        Assert.Null(_companions.Talk());

        _tide.Milestone("outro");
        Assert.Equal("conversa.2", _companions.Talk());
        _tide.Milestone("mais_um");
        Assert.Equal("conversa.2", _companions.Talk());
        Assert.Equal(3, _companions.Bond);
    }

    [Fact]
    public void Save_KeepsBondAndTalks()
    {
        _companions.Choose("aulo");
        _companions.Talk();
        var loaded = SaveGame.Read(SaveGame.Write(_state));
        Assert.Equal((1, 1, (int?)1), (loaded.CompanionBond, loaded.CompanionTalks, loaded.CompanionTalkStep));
    }

    // ---- Na luta ----

    private static BattleFlow Flow(SupportEffect effect) =>
        new(new Gauge(100f), new Gauge(60f), new ShadeResolve(100f),
            new[] { new ShadeAttack(ShadeAttackKind.Direct, "anel_basico", "a", new GradeValues(0f, 5f, 10f)) },
            new CompanionSupport("x", "y", effect, 1, 0.4f, 0.5f));

    [Fact]
    public void Support_AfterAReprovedVia_IsReluctant_OnlyOnce()
    {
        var flow = Flow(SupportEffect.WidenDefense);
        flow.ReproveAction();
        flow.EndPlayerAction();
        Assert.True(flow.SupportReluctant);
        flow.EndSupport();
        Assert.Equal(0.2f, flow.SupportPower(SupportEffect.WidenDefense));
        Assert.Equal(0f, flow.SupportPower(SupportEffect.WidenListen));
        flow.EndShadeTurn();
        Assert.Equal(0f, flow.SupportPower(SupportEffect.WidenDefense));

        flow.EndPlayerAction();
        flow.EndSupport();
        Assert.Equal(0.4f, flow.SupportPower(SupportEffect.WidenDefense));
    }

    [Fact]
    public void Support_ForThePlayer_LastsThroughTheNextPlayerAction()
    {
        var flow = Flow(SupportEffect.CheaperMemory);
        flow.EndPlayerAction();
        flow.EndSupport();
        flow.EndShadeTurn();
        Assert.Equal(0.4f, flow.SupportPower(SupportEffect.CheaperMemory));

        flow.EndPlayerAction();
        // Intervalo 1: apoia de novo logo depois.
        Assert.Equal(BattlePhase.CompanionSupport, flow.Phase);
        Assert.False(flow.SupportActive);
    }
}
