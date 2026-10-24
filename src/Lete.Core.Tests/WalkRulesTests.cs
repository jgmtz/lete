using System.Numerics;
using Lete.Core.Movement;

namespace Lete.Core.Tests;

public class WalkRulesTests
{
    [Theory]
    [InlineData(0, -1, ScreenDirection.Up)]
    [InlineData(0, 1, ScreenDirection.Down)]
    [InlineData(-1, 0, ScreenDirection.Left)]
    [InlineData(1, 0, ScreenDirection.Right)]
    public void SingleAxis_PicksMatchingDirection(float x, float y, ScreenDirection expected)
    {
        Assert.Equal(expected, WalkRules.Resolve(new Vector2(x, y), ScreenDirection.Down));
    }

    [Theory]
    [InlineData(1, -1, ScreenDirection.Right)]
    [InlineData(-1, -1, ScreenDirection.Left)]
    [InlineData(1, 1, ScreenDirection.Right)]
    [InlineData(-1, 1, ScreenDirection.Left)]
    public void ExactDiagonal_KeepsHorizontalView(float x, float y, ScreenDirection expected)
    {
        Assert.Equal(expected, WalkRules.Resolve(new Vector2(x, y), ScreenDirection.Up));
    }

    [Fact]
    public void AnalogMostlyVertical_PicksVerticalView()
    {
        Assert.Equal(ScreenDirection.Up, WalkRules.Resolve(new Vector2(0.3f, -0.9f), ScreenDirection.Down));
    }

    [Fact]
    public void NoInput_KeepsCurrentDirection()
    {
        Assert.Equal(ScreenDirection.Left, WalkRules.Resolve(Vector2.Zero, ScreenDirection.Left));
    }

    [Fact]
    public void Diagonal_IsLimitedToUnitLength()
    {
        var limited = WalkRules.LimitLength(new Vector2(1, 1));

        Assert.Equal(1f, limited.Length(), 5);
    }

    [Fact]
    public void PartialAnalog_IsNotScaledUp()
    {
        var input = new Vector2(0.3f, 0.4f);

        Assert.Equal(input, WalkRules.LimitLength(input));
    }
}
