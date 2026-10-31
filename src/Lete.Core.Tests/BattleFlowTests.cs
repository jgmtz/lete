using System.Numerics;
using Lete.Core.Combat;
using Lete.Core.Combat.Gestures;
using Lete.Core.Data;
using Lete.Core.Systems;

namespace Lete.Core.Tests;

public class BattleFlowTests
{
    private static readonly GradeValues Damage = new(0f, 10f, 20f);

    private static readonly ShadeAttack[] Cycle =
    {
        new(ShadeAttackKind.Direct, "aparo", "golpe", Damage),
        new(ShadeAttackKind.Pattern, "esquiva", "ondas", Damage),
    };

    private static BattleFlow NewFlow(CompanionSupport? support = null) =>
        new(new Gauge(100f), new Gauge(60f), new ShadeResolve(100f), Cycle, support);

    [Fact]
    public void Round_GoesPlayerThenShade()
    {
        var flow = NewFlow();
        Assert.Equal(BattlePhase.PlayerTurn, flow.Phase);

        flow.EndPlayerAction();
        Assert.Equal(BattlePhase.ShadeTurn, flow.Phase);

        flow.EndShadeTurn();
        Assert.Equal(BattlePhase.PlayerTurn, flow.Phase);
        Assert.Equal(2, flow.Round);
    }

    [Fact]
    public void Companion_SupportsEverySecondRound_UntilEndOfShadeTurn()
    {
        var flow = NewFlow(new CompanionSupport("aulo", "postura", SupportEffect.WidenDefense, 2, 0.15f));
        Assert.Equal(1, flow.RoundsUntilSupport);

        flow.EndPlayerAction();
        Assert.Equal(BattlePhase.ShadeTurn, flow.Phase);
        flow.EndShadeTurn();

        Assert.Equal(0, flow.RoundsUntilSupport);
        flow.EndPlayerAction();
        Assert.Equal(BattlePhase.CompanionSupport, flow.Phase);
        flow.EndSupport();
        Assert.True(flow.SupportActive);
        Assert.Equal(BattlePhase.ShadeTurn, flow.Phase);

        flow.EndShadeTurn();
        Assert.False(flow.SupportActive);
        Assert.Equal(1, flow.RoundsUntilSupport);
    }

    [Fact]
    public void ShadeAttacks_RepeatTheirCycle()
    {
        var flow = NewFlow();
        Assert.Equal(ShadeAttackKind.Direct, flow.NextShadeAttack.Kind);
        flow.EndPlayerAction();
        flow.EndShadeTurn();
        Assert.Equal(ShadeAttackKind.Pattern, flow.NextShadeAttack.Kind);
        flow.EndPlayerAction();
        flow.EndShadeTurn();
        Assert.Equal(ShadeAttackKind.Direct, flow.NextShadeAttack.Kind);
    }

    [Fact]
    public void BrokenShade_EndsInVictoryRightAfterPlayerAction()
    {
        var flow = NewFlow();
        flow.Resolve.Reduce(100f);
        flow.EndPlayerAction();
        Assert.Equal(BattlePhase.Victory, flow.Phase);
        Assert.True(flow.IsOver);
    }

    [Fact]
    public void EmptyBreath_EndsInDefeatAfterShadeTurn()
    {
        var flow = NewFlow();
        flow.EndPlayerAction();
        flow.Breath.Reduce(150f);
        flow.EndShadeTurn();
        Assert.Equal(BattlePhase.Defeat, flow.Phase);
        Assert.Equal(1, flow.Round);
    }

    [Fact]
    public void Flow_RejectsStepsOutOfOrder()
    {
        var flow = NewFlow();
        Assert.Throws<InvalidOperationException>(() => flow.EndShadeTurn());
        Assert.Throws<InvalidOperationException>(() => flow.EndSupport());
    }

    [Fact]
    public void Gauge_StaysWithinZeroAndMax()
    {
        var gauge = new Gauge(50f);
        gauge.Reduce(80f);
        Assert.True(gauge.IsEmpty);
        gauge.Restore(70f);
        Assert.Equal(50f, gauge.Current);
        Assert.Throws<ArgumentOutOfRangeException>(() => gauge.Reduce(-1f));
    }

    [Fact]
    public void Windows_WidenedLowerBothThresholds()
    {
        var widened = new GestureWindows(0.8f, 0.4f).Widened(0.15f);
        Assert.Equal(0.65f, widened.Perfect, 3);
        Assert.Equal(0.25f, widened.Good, 3);
        Assert.Equal(GestureGrade.Good, widened.Grade(0.3f));
        Assert.Equal(0f, new GestureWindows(0.1f, 0.05f).Widened(0.5f).Good);
    }

    private static readonly DodgeSettings Box = new(5f, 100f, 60f, 60f, 2f, 3f, 0.6f, 4);

    private sealed class FixedBullets(params Vector2[] bullets) : IDodgePattern
    {
        public IEnumerable<Vector2> BulletsAt(float seconds) => bullets;

        public IDodgePattern Slowed(float factor) => this;
    }

    [Fact]
    public void Waves_SlowedMoveLessInTheSameTime()
    {
        var waves = new WavesPattern(Box, 1f, 40f, 5, 0f, 0f, 0f);
        var normal = waves.BulletsAt(1.5f).Min(b => b.X);
        var slow = waves.Slowed(2f).BulletsAt(1.5f).Min(b => b.X);
        Assert.True(slow > normal);
    }

    [Fact]
    public void Dodge_MarkerStaysInsideBox()
    {
        var run = new DodgeRun(Box, new FixedBullets());
        for (var i = 0; i < 100; i++)
        {
            run.Step(0.05f, new Vector2(-1f, -1f));
        }

        Assert.Equal(new Vector2(2f, 2f), run.Marker);
        Assert.True(run.IsFinished);
        Assert.Equal(1f, run.Score);
    }

    [Fact]
    public void Dodge_HitCountsOnceDuringInvulnerability()
    {
        var run = new DodgeRun(Box, new FixedBullets(new Vector2(50f, 30f)));
        Assert.True(run.Step(0.1f, Vector2.Zero));
        Assert.False(run.Step(0.1f, Vector2.Zero));
        Assert.Equal(1, run.Hits);

        for (var i = 0; i < 6; i++)
        {
            run.Step(0.1f, Vector2.Zero);
        }

        Assert.Equal(2, run.Hits);
        Assert.Equal(0.5f, run.Score);
    }

    [Fact]
    public void Waves_LeaveOneRowOpenPerColumn()
    {
        var waves = new WavesPattern(Box, 1f, 40f, 5, 0f, 0f, 0f);
        Assert.DoesNotContain(waves.BulletsAt(-0.5f), b => b.X < Box.BoxWidth);
        Assert.Equal(4, waves.BulletsAt(0.5f).Count());
        Assert.NotEqual(waves.GapRow(0), waves.GapRow(1));
    }

    [Fact]
    public void Data_ReadsShadeCycleCompanionAndDodge()
    {
        var root = Path.Combine(RepoRoot(), "content");
        var attacks = CombatData.ShadeAttacksFromJson(File.ReadAllText(Path.Combine(root, "shades", "sombra_de_teste.json")));
        Assert.Contains(attacks, a => a.Kind == ShadeAttackKind.Pattern);

        var aulo = CompanionData.FromJson(File.ReadAllText(Path.Combine(root, "companions", "aulo.json")));
        Assert.Equal(("postura", SupportEffect.WidenDefense), (aulo.SupportId, aulo.Effect));

        var protagonist = CombatData.ProtagonistFromJson(File.ReadAllText(Path.Combine(root, "battle", "teste_ataque.json")));
        Assert.Equal("sombra_f", protagonist.Id);

        foreach (var attack in attacks)
        {
            var gesture = File.ReadAllText(Path.Combine(root, "gestures", attack.Gesture + ".json"));
            if (attack.Kind == ShadeAttackKind.Pattern)
            {
                Assert.NotNull(GestureData.DodgeFromJson(gesture).Pattern);
            }
            else
            {
                Assert.True(GestureData.RingFromJson(gesture).ShrinkSeconds > 0f);
            }
        }
    }

    [Fact]
    public void Protagonist_MustStandRightmost()
    {
        var setup = BattleData.FromJson(
            File.ReadAllText(Path.Combine(RepoRoot(), "content", "battle", "teste_ataque.json")),
            CastData.FromJson(File.ReadAllText(Path.Combine(RepoRoot(), "content", "characters", "elenco.json"))));
        Assert.Null(setup.ProtagonistSlotError("sombra_f"));
        Assert.Contains("direita", setup.ProtagonistSlotError("aulo"));
        Assert.NotNull(setup.ProtagonistSlotError("ninguem"));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "project.godot")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("project.godot não encontrado.");
    }
}
