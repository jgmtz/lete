using System.Numerics;
using Lete.Core.Combat;
using Lete.Core.Data;

namespace Lete.Core.Tests;

public class HudAndTurnTests
{
    private static readonly ScreenRect Gestures = new(0.33f, 0.24f, 0.34f, 0.36f);

    [Fact]
    public void Turn_CyclesThroughParticipants()
    {
        var order = new TurnOrder(new[] { "aulo", "ismena" });

        Assert.Equal("aulo", order.Current);
        order.Advance();
        Assert.Equal("ismena", order.Current);
        order.Advance();
        Assert.Equal("aulo", order.Current);
    }

    [Fact]
    public void Turn_RejectsEmptyOrder()
    {
        Assert.Throws<ArgumentException>(() => new TurnOrder(Array.Empty<string>()));
    }

    [Fact]
    public void Understanding_ThirdObolNeedsNameAndFixation()
    {
        var understanding = new ShadeUnderstanding();
        Assert.Equal(0, understanding.Points);

        understanding.RevealName();
        Assert.Equal(1, understanding.Points);
        Assert.False(understanding.IsComplete);

        understanding.RevealFixation();
        Assert.Equal(ShadeUnderstanding.MaxPoints, understanding.Points);
        Assert.True(understanding.IsComplete);
    }

    [Fact]
    public void Rect_DetectsOverlap()
    {
        Assert.True(new ScreenRect(0.6f, 0.5f, 0.3f, 0.3f).Intersects(Gestures));
        Assert.False(new ScreenRect(0.74f, 0.46f, 0.22f, 0.46f).Intersects(Gestures));
    }

    [Fact]
    public void Layouts_RejectMenuOverGestureArea()
    {
        var bad = new HudLayout("x", new ScreenRect(0.5f, 0.5f, 0.4f, 0.4f), new ScreenRect(0f, 0.9f, 0.2f, 0.05f));

        Assert.Contains(new HudLayoutSet(Gestures, new[] { bad }).Validate(), e => e.Contains("menu invade"));
    }

    [Fact]
    public void Layouts_RejectPanelOutsideScreen()
    {
        var bad = new HudLayout("x", new ScreenRect(0.8f, 0.8f, 0.3f, 0.1f), new ScreenRect(0f, 0.9f, 0.2f, 0.05f));

        Assert.Contains(new HudLayoutSet(Gestures, new[] { bad }).Validate(), e => e.Contains("fora da tela"));
    }

    [Fact]
    public void DepthScale_ShrinksTheBackUpToTheLimit()
    {
        var layout = new BattleLayout(
            new[] { new Vector2(-1, -1.2f), new Vector2(-2, -0.8f) },
            new[] { new Vector2(1, -0.4f) },
            DepthScale: 0.06f);

        Assert.Equal(1f, layout.ScaleAt(new Vector2(-1, -1.2f)), 5);
        Assert.Equal(0.94f, layout.ScaleAt(new Vector2(1, -0.4f)), 5);
        Assert.Equal(0.97f, layout.ScaleAt(new Vector2(-2, -0.8f)), 5);
    }

    [Fact]
    public void DepthScale_AboveTenPercentIsRejected()
    {
        var layout = new BattleLayout(new[] { new Vector2(-1, -1) }, new[] { new Vector2(1, 0) }, DepthScale: 0.2f);

        Assert.Contains(layout.Validate(), e => e.Contains("depth_scale"));
    }

    [Fact]
    public void ContentFiles_AreValid()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "project.godot")))
        {
            dir = dir.Parent;
        }

        var set = HudData.FromJson(File.ReadAllText(Path.Combine(dir!.FullName, "content/battle/hud_layouts.json")));
        Assert.Equal(new[] { "normal" }, set.Layouts.Select(l => l.Id));

        BattleData.FromJson(File.ReadAllText(Path.Combine(dir.FullName, "content/battle/teste_ataque.json")));
    }
}
