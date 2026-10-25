using Lete.Core.Combat.Gestures;
using Lete.Core.Data;

namespace Lete.Core.Tests;

public class GestureTests
{
    private static readonly RingTiming Ring = new(ShrinkSeconds: 1f, StartScale: 3f, ZeroScoreOffsetSeconds: 0.25f);

    [Fact]
    public void Ring_PressAtMeeting_ScoresOne()
    {
        Assert.Equal(1f, Ring.Score(1f), 5);
    }

    [Theory]
    [InlineData(0.9f)]
    [InlineData(1.1f)]
    public void Ring_EarlyAndLate_ScoreTheSame(float pressedAt)
    {
        Assert.Equal(0.6f, Ring.Score(pressedAt), 4);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(1.25f)]
    [InlineData(2f)]
    public void Ring_PressOutsideWindow_ScoresZero(float pressedAt)
    {
        Assert.Equal(0f, Ring.Score(pressedAt));
    }

    [Fact]
    public void Ring_ShrinksFromStartScaleToCircle()
    {
        Assert.Equal(3f, Ring.ScaleAt(0f), 5);
        Assert.Equal(2f, Ring.ScaleAt(0.5f), 5);
        Assert.Equal(1f, Ring.ScaleAt(1f), 5);
    }

    [Fact]
    public void Ring_ExpiresAfterZeroScoreOffset()
    {
        Assert.False(Ring.IsExpired(1.25f));
        Assert.True(Ring.IsExpired(1.26f));
    }

    [Theory]
    [InlineData(1f, GestureGrade.Perfect)]
    [InlineData(0.8f, GestureGrade.Perfect)]
    [InlineData(0.79f, GestureGrade.Good)]
    [InlineData(0.4f, GestureGrade.Good)]
    [InlineData(0.39f, GestureGrade.Failed)]
    [InlineData(0f, GestureGrade.Failed)]
    public void Windows_GradeByThreshold(float score, GestureGrade expected)
    {
        Assert.Equal(expected, new GestureWindows(perfect: 0.8f, good: 0.4f).Grade(score));
    }

    [Fact]
    public void Windows_RejectPerfectBelowGood()
    {
        Assert.Throws<ArgumentException>(() => new GestureWindows(perfect: 0.3f, good: 0.5f));
    }

    [Fact]
    public void Data_ReadsTbdAndPlainNumbers()
    {
        var windows = GestureData.WindowsFromDifficulty(
            """{ "gesture_windows": { "perfect": { "_tbd": true, "value": 0.9 }, "good": 0.5 } }""");

        Assert.Equal(0.9f, windows.Perfect, 5);
        Assert.Equal(0.5f, windows.Good, 5);
    }

    [Fact]
    public void Data_ReadsRingTiming()
    {
        var ring = GestureData.RingFromJson(
            """{ "shrink_seconds": 1.2, "start_scale": { "_tbd": true, "value": 3 }, "zero_score_offset_seconds": 0.25 }""");

        Assert.Equal(new RingTiming(1.2f, 3f, 0.25f), ring);
    }

    [Fact]
    public void Data_MissingField_FailsClearly()
    {
        var error = Assert.Throws<FormatException>(() => GestureData.RingFromJson("""{ "shrink_seconds": 1 }"""));

        Assert.Contains("start_scale", error.Message);
    }

    [Fact]
    public void ContentFiles_Load()
    {
        var root = RepoRoot();

        GestureData.WindowsFromDifficulty(File.ReadAllText(Path.Combine(root, "content/difficulty/estige.json")));
        var ring = File.ReadAllText(Path.Combine(root, "content/gestures/anel_basico.json"));
        GestureData.RingFromJson(ring);
        CombatData.ResolveDamageFromJson(ring);
        CombatData.ShadeResolveFromJson(File.ReadAllText(Path.Combine(root, "content/shades/sombra_de_teste.json")));
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
