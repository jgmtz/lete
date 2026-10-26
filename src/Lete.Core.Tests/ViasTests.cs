using System.Numerics;
using Lete.Core.Combat;
using Lete.Core.Combat.Gestures;

namespace Lete.Core.Tests;

public class ViasTests
{
    private static readonly GradeValues Damage = new(0f, 10f, 20f);

    private static ShadeAttack Attack(string caption) => new(ShadeAttackKind.Direct, "aparo", caption, Damage);

    private static BattleFlow NewFlow() =>
        new(new Gauge(100f), new Gauge(60f), new ShadeResolve(100f), new[] { Attack("a"), Attack("b"), Attack("c") }, null);

    [Theory]
    [InlineData(BattleOutcome.Spared)]
    [InlineData(BattleOutcome.Fled)]
    [InlineData(BattleOutcome.Yielded)]
    public void ImmediateVias_EndTheBattleWhenTheActionEnds(BattleOutcome outcome)
    {
        var flow = NewFlow();
        Assert.False(flow.IsEnding);
        flow.EndBattle(outcome);
        Assert.True(flow.IsEnding);
        Assert.Equal(BattlePhase.PlayerTurn, flow.Phase);
        flow.EndPlayerAction();
        Assert.Equal(BattlePhase.Victory, flow.Phase);
        Assert.Equal(outcome, flow.Outcome);
    }

    [Fact]
    public void RuleOutcomes_CannotBeChosenDirectly()
    {
        Assert.Throws<ArgumentException>(() => NewFlow().EndBattle(BattleOutcome.Freed));
    }

    [Fact]
    public void Witnessing_EndsAfterAFullCycle()
    {
        var flow = NewFlow();
        flow.StartWitnessing();
        for (var i = 0; i < 2; i++)
        {
            flow.EndPlayerAction();
            flow.EndShadeTurn();
            Assert.Equal(BattlePhase.PlayerTurn, flow.Phase);
        }

        Assert.Equal(1, flow.WitnessTurnsLeft);
        flow.EndPlayerAction();
        flow.EndShadeTurn();
        Assert.Equal(BattleOutcome.Witnessed, flow.Outcome);
        Assert.False(flow.IsWitnessing);
    }

    [Fact]
    public void Witnessing_DefeatWinsOverTheLastTurn()
    {
        var flow = NewFlow();
        flow.StartWitnessing();
        flow.EndPlayerAction();
        flow.EndShadeTurn();
        flow.EndPlayerAction();
        flow.EndShadeTurn();
        flow.EndPlayerAction();
        flow.Breath.Reduce(100f);
        flow.EndShadeTurn();
        Assert.Equal(BattlePhase.Defeat, flow.Phase);
    }

    [Fact]
    public void Witnessing_CanBeAbandoned()
    {
        var flow = NewFlow();
        flow.StartWitnessing();
        flow.StopWitnessing();
        Assert.Null(flow.WitnessTurnsLeft);
    }

    [Fact]
    public void Phase_ChangesTheCycleFromItsFirstAttack()
    {
        var flow = NewFlow();
        flow.EndPlayerAction();
        flow.EndShadeTurn();
        Assert.Equal("b", flow.NextShadeAttack.CaptionKey);

        flow.ChangeCycle(new[] { Attack("x"), Attack("y") });
        Assert.Equal("x", flow.NextShadeAttack.CaptionKey);
        Assert.Equal(2, flow.CycleLength);
    }

    [Fact]
    public void Narration_EachVoiceAndPhaseOnce()
    {
        var story = new ShadeStory(
            new[] { new ShadeVoice(0.75f, "v1"), new ShadeVoice(0.5f, "v2") },
            new[] { new ShadePhase(0.5f, new[] { Attack("x") }, "fase", ListenReveal.Fixation) },
            new[] { "h" }, "fuga", new[] { "c" }, 10f, new[] { "p1", "p2" }, "poupar", null);
        var narration = new ShadeNarration(story);

        Assert.Empty(narration.NewVoices(0.9f));
        Assert.Equal(new[] { "v1", "v2" }, narration.NewVoices(0.4f).Select(v => v.TextKey));
        Assert.Empty(narration.NewVoices(0.1f));

        Assert.NotNull(narration.NewPhase(0.4f));
        Assert.Null(narration.NewPhase(0.3f));

        Assert.Equal("p1", ShadeNarration.DefeatClue(story, 1));
        Assert.Equal("p2", ShadeNarration.DefeatClue(story, 5));
    }

    private static BattleBag NewBag() => new(
        12,
        new Dictionary<string, int> { ["agua"] = 1, ["mel"] = 2 },
        new[] { new ProvisionDefinition("pao", "n", "d", 30f, 0) },
        new Dictionary<string, int> { ["pao"] = 1 });

    [Fact]
    public void Defeat_LeavesObolsAndOfferingsWithTheShade_UntilShesBeaten()
    {
        var bag = NewBag();
        var hoard = new ShadeHoard();
        Assert.Equal(12, hoard.TakeFrom(bag));
        Assert.Equal(0, bag.Obols);
        Assert.False(bag.HasAnyOffering);
        Assert.True(bag.HasAnyProvision); // provisões não se perdem

        Assert.True(hoard.ReturnTo(bag));
        Assert.Equal(12, bag.Obols);
        Assert.Equal(2, bag.OfferingCount("mel"));
        Assert.False(hoard.ReturnTo(bag));
    }

    [Fact]
    public void Bag_SpendsOfferingsAndProvisions()
    {
        var bag = NewBag();
        bag.SpendOffering("agua");
        Assert.Throws<InvalidOperationException>(() => bag.SpendOffering("agua"));
        Assert.Equal(30f, bag.UseProvision("pao").BreathRestore);
        Assert.False(bag.HasAnyProvision);
    }

    private static readonly DodgeSettings Box = new(6f, 100f, 60f, 60f, 2f, 3f, 0.6f, 4);

    private sealed class NoBullets : IDodgePattern
    {
        public IEnumerable<Vector2> BulletsAt(float seconds) => Array.Empty<Vector2>();

        public IDodgePattern Slowed(float factor) => this;
    }

    [Fact]
    public void Escape_ReachingTheExitSucceeds_TimeoutFails()
    {
        var exit = new EscapeExit(0f, 0.35f, 0.08f, 0.3f);
        var run = new DodgeRun(Box, new NoBullets(), exit);
        while (!run.IsFinished)
        {
            run.Step(0.1f, new Vector2(-1f, 0f));
        }

        Assert.True(run.Escaped);
        Assert.Equal(1f, run.Score);

        var stuck = new DodgeRun(Box, new NoBullets(), exit);
        while (!stuck.IsFinished)
        {
            stuck.Step(0.1f, new Vector2(1f, 0f));
        }

        Assert.False(stuck.Escaped);
        Assert.Equal(0f, stuck.Score);
    }

    [Fact]
    public void Assist_AutomaticIsAlwaysGood_AndCycles()
    {
        var windows = new GestureWindows(0.8f, 0.4f);
        Assert.Empty(new AssistSettings(0.2f, 0.6f).Validate(windows));
        Assert.NotEmpty(new AssistSettings(0.2f, 0.9f).Validate(windows));
        Assert.Equal(0.6f, new AssistSettings(0.2f, 0.6f).WindowsFor(GestureAssist.Wider, windows).Perfect, 3);
        Assert.Equal(GestureAssist.Automatic, AssistSettings.Next(GestureAssist.Wider));
        Assert.Equal(GestureAssist.Off, AssistSettings.Next(GestureAssist.Automatic));
    }
}
