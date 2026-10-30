using Lete.Core.Combat;
using Lete.Core.Combat.Gestures;
using Lete.Core.Data;

namespace Lete.Core.Tests;

public class MemoryTests
{
    private static readonly StrokePattern Curve = new(
        new[] { StrokeDirection.Right, StrokeDirection.UpRight, StrokeDirection.Up }, 0.5f, 0.2f, 1f);

    private static MemorySkill Skill(string id, MemoryEffectKind effect = MemoryEffectKind.Heal) =>
        new(id, RecollectionKind.Childhood, "n", "d", Curve, 12f, 1.5f, effect, new GradeValues(15f, 10f, 5f));

    [Fact]
    public void Stroke_OnBeatAndRightDirectionIsPerfect()
    {
        var run = new StrokeRun(Curve);
        run.Press(StrokeDirection.Right, 1f);
        run.Press(StrokeDirection.UpRight, 1.5f);
        run.Press(StrokeDirection.Up, 2f);
        Assert.True(run.IsFinished);
        Assert.Equal(1f, run.Score, 3);
    }

    [Fact]
    public void Stroke_WrongDirectionLateAndMissedStepsLoseValue()
    {
        var run = new StrokeRun(Curve);
        run.Press(StrokeDirection.Left, 1f);       // direção errada: 0
        run.Press(StrokeDirection.UpRight, 1.6f);  // 0,1 s atrasado de 0,2: 0,5
        run.Advance(2.5f);                         // terceiro passo perdido: 0
        Assert.True(run.IsFinished);
        Assert.Equal(0.5f / 3f, run.Score, 3);
    }

    [Fact]
    public void Stroke_IgnoresPressesTooEarly()
    {
        var run = new StrokeRun(Curve);
        run.Press(StrokeDirection.Right, 0.2f);
        Assert.Equal(0, run.CurrentStep);
    }

    [Fact]
    public void Stroke_ReadsDiagonalsFromAxes()
    {
        Assert.Equal(StrokeDirection.UpRight, StrokeRun.FromAxes(1, -1));
        Assert.Equal(StrokeDirection.Down, StrokeRun.FromAxes(0, 1));
        Assert.Null(StrokeRun.FromAxes(0, 0));
    }

    [Fact]
    public void Stroke_RejectsTooManyStepsOrOverlappingWindows()
    {
        Assert.NotEmpty((Curve with { Steps = new[] { StrokeDirection.Up } }).Validate());
        Assert.NotEmpty((Curve with { ZeroScoreOffsetSeconds = 0.3f }).Validate());
    }

    [Fact]
    public void Memory_FailedStrokeCostsMore()
    {
        var book = new MemoryBook(new[] { Skill("colo") });
        var lucidity = new Gauge(60f);
        Assert.Equal(12f, book.Use("colo", GestureGrade.Good, lucidity).Cost);
        Assert.Equal(18f, book.Use("colo", GestureGrade.Failed, lucidity).Cost);
        Assert.Equal(30f, lucidity.Current);
    }

    [Fact]
    public void Memory_EmptyLucidityForgetsTheMostUsed()
    {
        var book = new MemoryBook(new[] { Skill("colo"), Skill("mesa", MemoryEffectKind.Guard) });
        var lucidity = new Gauge(40f);
        book.Use("colo", GestureGrade.Good, lucidity);
        book.Use("colo", GestureGrade.Good, lucidity);
        book.Use("mesa", GestureGrade.Good, lucidity);
        Assert.Null(book.CheckForgetting(lucidity));

        book.Use("mesa", GestureGrade.Good, lucidity); // só sobravam 4: gasta o que tem e zera
        Assert.True(lucidity.IsEmpty);

        // Empate (2 a 2): esquece a usada por último.
        var forgetting = book.CheckForgetting(lucidity)!;
        Assert.Equal(("mesa", false), (forgetting.Skill.Id, forgetting.Resisted));
        Assert.True(book.IsForgotten("mesa"));
        Assert.Throws<InvalidOperationException>(() => book.Use("mesa", GestureGrade.Good, new Gauge(50f)));

        book.Rest();
        Assert.False(book.IsForgotten("mesa"));
    }

    [Fact]
    public void Memory_LightnessProtectionHoldsAForgetting_RenewsOnRest()
    {
        var book = new MemoryBook(new[] { Skill("colo") }, protections: 1);
        var lucidity = new Gauge(12f);
        book.Use("colo", GestureGrade.Good, lucidity);

        Assert.Equal(new Forgetting(book.Skills[0], Resisted: true), book.CheckForgetting(lucidity));
        Assert.False(book.IsForgotten("colo"));
        Assert.Equal(0, book.ProtectionsLeft);

        var again = new Gauge(12f);
        book.Use("colo", GestureGrade.Good, again);
        Assert.False(book.CheckForgetting(again)!.Resisted);
        Assert.True(book.IsForgotten("colo"));

        book.Rest();
        Assert.Equal(1, book.ProtectionsLeft);
    }

    [Fact]
    public void Memory_NeedsLucidityForTheNormalCost()
    {
        var book = new MemoryBook(new[] { Skill("colo") });
        Assert.True(book.CanUse("colo", new Gauge(12f)));
        var low = new Gauge(20f);
        low.Reduce(10f);
        Assert.False(book.CanUse("colo", low));
    }

    [Fact]
    public void Flow_MemoryEffectsLastUntilTheShadeTurnEnds()
    {
        var attacks = new[] { new ShadeAttack(ShadeAttackKind.Direct, "aparo", "golpe", new GradeValues(0f, 10f, 20f)) };
        var flow = new BattleFlow(new Gauge(100f), new Gauge(60f), new ShadeResolve(100f), attacks, null);
        flow.ApplyUntilShadeTurnEnds(MemoryEffectKind.Guard, 0.4f);
        flow.ApplyUntilShadeTurnEnds(MemoryEffectKind.Guard, 0.2f);
        flow.ApplyUntilShadeTurnEnds(MemoryEffectKind.Slow, 1.3f);
        flow.ApplyUntilShadeTurnEnds(MemoryEffectKind.Focus, 0.12f);
        Assert.Equal(0.4f, flow.GuardFraction);
        Assert.Throws<ArgumentException>(() => flow.ApplyUntilShadeTurnEnds(MemoryEffectKind.Heal, 1f));

        flow.EndPlayerAction();
        flow.EndShadeTurn();
        Assert.Equal(0f, flow.GuardFraction);
        Assert.Equal(1f, flow.ShadeSlowFactor);
        Assert.Equal(0f, flow.FocusWidening);
    }

    [Fact]
    public void Data_LoadsFiveTestMemoriesOnePerKind()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "project.godot")))
        {
            dir = dir.Parent;
        }

        var skills = MemoryData.FromJson(File.ReadAllText(Path.Combine(dir!.FullName, "content", "memories", "teste.json")));
        Assert.Equal(5, skills.Count);
        Assert.Equal(5, skills.Select(s => s.Kind).Distinct().Count());
        Assert.Equal(5, skills.Select(s => s.Effect).Distinct().Count());
    }
}
