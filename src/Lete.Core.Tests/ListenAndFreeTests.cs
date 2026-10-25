using System.Numerics;
using Lete.Core.Combat;
using Lete.Core.Combat.Gestures;
using Lete.Core.Data;

namespace Lete.Core.Tests;

public class ListenAndFreeTests
{
    private static readonly ListenEntry[] Entries =
    {
        new(0, ListenReveal.Clue, "pista", "pista.metade"),
        new(1, ListenReveal.Name, "nome", "nome.metade"),
        new(2, ListenReveal.Fixation, "fixacao", "fixacao.metade"),
    };

    private static readonly LiberationRules Rules = new(
        "culpa",
        new[] { new FixationOption("saudade", "l1", "corrige.saudade"), new FixationOption("culpa", "l2", "") },
        100f,
        new GradeValues(50f, 35f, 15f),
        15f,
        1.5f,
        "progresso",
        "parte");

    [Fact]
    public void Hold_ScoresByDistanceToTarget()
    {
        var hold = new HoldTiming(2f, 0.75f, 0.25f, 3f);
        Assert.Equal(0.5f, hold.FillAt(1f));
        Assert.Equal(1f, hold.Score(0.75f));
        Assert.Equal(0.5f, hold.Score(0.625f), 3);
        Assert.Equal(0f, hold.Score(0.2f));
        Assert.True(hold.IsFull(2f));
        Assert.NotEmpty(new HoldTiming(1f, 1.2f, 0.25f, 3f).Validate());
    }

    [Fact]
    public void Drag_FollowingThePathScoresHigh_CuttingAcrossScoresLow()
    {
        var settings = new DragSettings(100f, 40f, new[] { new Vector2(0f, 0.5f), new Vector2(1f, 0.5f) }, 20f, 2f, 8f, 30f);

        var straight = new DragRun(settings);
        while (!straight.IsFinished)
        {
            straight.Step(0.1f, Vector2.UnitX);
        }

        Assert.True(straight.ReachedEnd);
        Assert.Equal(1f, straight.Score, 3);

        var wobbly = new DragRun(settings);
        for (var i = 0; i < 20; i++)
        {
            wobbly.Step(0.1f, new Vector2(0f, -1f));
        }

        while (!wobbly.IsFinished)
        {
            wobbly.Step(0.1f, Vector2.Normalize(new Vector2(1f, wobbly.Marker.Y < 20f ? 0.2f : 0f)));
        }

        Assert.True(wobbly.Score < straight.Score);
    }

    [Fact]
    public void Drag_TimeoutIsFailed()
    {
        var settings = new DragSettings(100f, 40f, new[] { new Vector2(0f, 0.5f), new Vector2(1f, 0.5f) }, 20f, 2f, 8f, 1f);
        var run = new DragRun(settings);
        run.Step(2f, Vector2.Zero);
        Assert.True(run.IsFinished);
        Assert.Equal(0f, run.Score);
    }

    [Fact]
    public void Listen_DependsOnCycleStepAndGrade()
    {
        var understanding = new ShadeUnderstanding();
        var listening = new ShadeListening(Entries, "murmurio", understanding);

        Assert.Equal("murmurio", listening.Listen(1, GestureGrade.Failed).TextKey);

        var half = listening.Listen(1, GestureGrade.Good);
        Assert.True(half.Partial);
        Assert.Equal("nome.metade", half.TextKey);
        Assert.False(understanding.NameRevealed);

        var full = listening.Listen(1, GestureGrade.Good);
        Assert.Equal(ListenReveal.Name, full.Revealed);
        Assert.True(understanding.NameRevealed);

        // Ouvir de novo repete a fala, sem revelar outra vez.
        Assert.Null(listening.Listen(1, GestureGrade.Perfect).Revealed);

        Assert.Equal(ListenReveal.Fixation, listening.Listen(2, GestureGrade.Perfect).Revealed);
        Assert.True(understanding.IsComplete);
    }

    [Fact]
    public void Liberation_NeedsFullUnderstandingAndLucidity()
    {
        var liberation = new Liberation(Rules);
        var understanding = new ShadeUnderstanding();
        var lucidity = new Gauge(20f);

        Assert.False(liberation.CanAttempt(understanding, lucidity));
        understanding.RevealName();
        understanding.RevealFixation();
        Assert.True(liberation.CanAttempt(understanding, lucidity));

        lucidity.Reduce(10f);
        Assert.False(liberation.CanAttempt(understanding, lucidity));
    }

    [Fact]
    public void Liberation_WrongChoiceIsRemoved_RightChoiceFillsPeace()
    {
        var liberation = new Liberation(Rules);
        var lucidity = new Gauge(100f);

        var wrong = liberation.Attempt("saudade", GestureGrade.Perfect, lucidity);
        Assert.False(wrong.Correct);
        Assert.Equal("corrige.saudade", wrong.TextKey);
        Assert.DoesNotContain(liberation.RemainingOptions, o => o.Id == "saudade");
        Assert.Equal(85f, lucidity.Current);

        Assert.Equal("progresso", liberation.Attempt("culpa", GestureGrade.Good, lucidity).TextKey);
        Assert.Equal(0.35f, liberation.Fraction, 3);
        Assert.Equal("progresso", liberation.Attempt("culpa", GestureGrade.Perfect, lucidity).TextKey);
        Assert.False(liberation.IsFreed);
        Assert.Equal("parte", liberation.Attempt("culpa", GestureGrade.Failed, lucidity).TextKey);
        Assert.True(liberation.IsFreed);
    }

    [Fact]
    public void Liberation_RulesRejectMissingCorrection()
    {
        var bad = Rules with { Options = new[] { new FixationOption("saudade", "l1", ""), new FixationOption("culpa", "l2", "") } };
        Assert.NotEmpty(bad.Validate());
    }

    [Fact]
    public void Flow_FreedShadeEndsInVictoryFreed_AndRageLastsOneShadeTurn()
    {
        var liberation = new Liberation(Rules);
        var attacks = new[] { new ShadeAttack(ShadeAttackKind.Direct, "aparo", "golpe", new GradeValues(0f, 10f, 20f)) };
        var flow = new BattleFlow(new Gauge(100f), new Gauge(100f), new ShadeResolve(100f), attacks, null, liberation);

        flow.Enrage();
        flow.EndPlayerAction();
        Assert.True(flow.Enraged);
        flow.EndShadeTurn();
        Assert.False(flow.Enraged);

        liberation.Attempt("culpa", GestureGrade.Perfect, flow.Lucidity);
        liberation.Attempt("culpa", GestureGrade.Perfect, flow.Lucidity);
        flow.EndPlayerAction();
        Assert.Equal(BattlePhase.Victory, flow.Phase);
        Assert.Equal(BattleOutcome.Freed, flow.Outcome);
    }

    [Fact]
    public void Data_LoadsTheRower()
    {
        var json = File.ReadAllText(Path.Combine(RepoRoot(), "content", "shades", "remador.json"));
        var (entries, murmur) = ShadeData.ListeningFromJson(json);
        Assert.Equal(3, entries.Count);
        Assert.False(string.IsNullOrEmpty(murmur));

        var rules = ShadeData.LiberationFromJson(json);
        Assert.Equal("culpa", rules.TrueFixation);
        Assert.Equal(3, rules.Options.Count);
    }

    [Fact]
    public void Data_RejectsListeningOutsideTheCycle()
    {
        const string json = """
            { "attacks": [ {} ], "murmur_key": "m",
              "listen": [ { "cycle_step": 3, "reveals": "name", "text_key": "a", "partial_key": "b" },
                          { "cycle_step": 0, "reveals": "fixation", "text_key": "a", "partial_key": "b" } ] }
            """;
        var error = Assert.Throws<FormatException>(() => ShadeData.ListeningFromJson(json));
        Assert.Contains("fora do ciclo", error.Message);
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
