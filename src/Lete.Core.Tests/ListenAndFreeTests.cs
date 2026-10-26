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

    // Caminho em L: 60 px para a direita, depois 40 px para baixo (100 px no total).
    private static readonly DragSettings Rail = new(
        100f, 40f, new[] { new Vector2(0f, 0f), new Vector2(0.6f, 0f), new Vector2(0.6f, 1f) }, 30f, 20f, 15f, 0.3f, 30f);

    [Fact]
    public void Drag_StaysOnTheRailAndTurnsTheCornerByItself()
    {
        var run = new DragRun(Rail);
        Assert.Equal(100f, run.Length, 3);

        // Segurando "direita" chega ao canto; depois "direita" já não aponta para a frente e a luz para.
        for (var i = 0; i < 30; i++)
        {
            run.Step(0.1f, Vector2.UnitX);
        }

        Assert.Equal(60f, run.Progress, 1);
        Assert.Equal(0f, run.Marker.Y, 3);

        // Para trás pela curva: "esquerda" volta pelo primeiro trecho.
        run.Step(0.5f, -Vector2.UnitX);
        Assert.True(run.Progress < 60f);
        run.Step(0.5f, Vector2.UnitX);

        // "Baixo" segue o segundo trecho, sempre em cima da linha.
        run.Step(0.5f, Vector2.UnitY);
        Assert.Equal(60f, run.Marker.X, 3);
        Assert.True(run.Marker.Y > 0f);
    }

    [Fact]
    public void Drag_KeepingPaceWithTheGuideScoresHigh_RushingScoresLow()
    {
        var calm = new DragRun(Rail);
        while (!calm.IsFinished)
        {
            // Como o demo: empurra só quando está atrás da guia.
            calm.Step(0.02f, calm.Gap < 0f ? calm.Tangent : Vector2.Zero);
        }

        var rushed = new DragRun(Rail);
        while (!rushed.IsFinished)
        {
            rushed.Step(0.02f, rushed.Tangent);
        }

        Assert.True(calm.ReachedEnd && rushed.ReachedEnd);
        Assert.True(calm.Score > 0.9f, $"calmo: {calm.Score}");
        Assert.True(rushed.Score < calm.Score, $"com pressa: {rushed.Score}");
    }

    [Fact]
    public void Drag_TimeoutIsFailed()
    {
        var run = new DragRun(Rail with { MaxSeconds = 1f });
        run.Step(2f, Vector2.Zero);
        Assert.True(run.IsFinished);
        Assert.Equal(0f, run.Score);
    }

    [Fact]
    public void Drag_RejectsAMarkerSlowerThanTheGuide()
    {
        Assert.NotEmpty((Rail with { MarkerSpeed = 10f }).Validate());
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
