using Lete.Core.Movement;

namespace Lete.Core.Tests;

public class FacingRulesTests
{
    [Theory]
    [InlineData(1, 1, Facing.SouthEast)]
    [InlineData(-1, 1, Facing.SouthWest)]
    [InlineData(1, -1, Facing.NorthEast)]
    [InlineData(-1, -1, Facing.NorthWest)]
    public void Diagonal_PicksMatchingFacing(float x, float y, Facing expected)
    {
        Assert.Equal(expected, FacingRules.Resolve(x, y, Facing.SouthEast));
    }

    [Fact]
    public void NoInput_KeepsCurrentFacing()
    {
        Assert.Equal(Facing.NorthWest, FacingRules.Resolve(0, 0, Facing.NorthWest));
    }

    [Theory]
    [InlineData(Facing.SouthWest, Facing.NorthWest)]
    [InlineData(Facing.SouthEast, Facing.NorthEast)]
    public void OnlyUp_TurnsToBackButKeepsSide(Facing current, Facing expected)
    {
        Assert.Equal(expected, FacingRules.Resolve(0, -1, current));
    }

    [Theory]
    [InlineData(Facing.NorthEast, Facing.NorthWest)]
    [InlineData(Facing.SouthEast, Facing.SouthWest)]
    public void OnlyLeft_KeepsFrontOrBack(Facing current, Facing expected)
    {
        Assert.Equal(expected, FacingRules.Resolve(-1, 0, current));
    }

    [Fact]
    public void OnlyTwoFacingsAreDrawn_TheOthersAreMirrors()
    {
        Assert.False(FacingRules.IsMirrored(Facing.SouthEast));
        Assert.False(FacingRules.IsMirrored(Facing.NorthEast));
        Assert.True(FacingRules.IsMirrored(Facing.SouthWest));
        Assert.True(FacingRules.IsMirrored(Facing.NorthWest));
    }
}
