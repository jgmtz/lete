using Lete.Core.Combat;
using Lete.Core.Combat.Gestures;
using Lete.Core.Data;

namespace Lete.Core.Tests;

public class CombatTests
{
    [Fact]
    public void Resolve_StartsFull()
    {
        var resolve = new ShadeResolve(100f);

        Assert.Equal(1f, resolve.Fraction);
        Assert.False(resolve.IsBroken);
    }

    [Fact]
    public void Resolve_ReducesAndNeverGoesNegative()
    {
        var resolve = new ShadeResolve(100f);

        resolve.Reduce(30f);
        Assert.Equal(0.7f, resolve.Fraction, 5);

        resolve.Reduce(500f);
        Assert.Equal(0f, resolve.Current);
        Assert.True(resolve.IsBroken);
    }

    [Fact]
    public void Resolve_RejectsInvalidValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShadeResolve(0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShadeResolve(10f).Reduce(-1f));
    }

    [Theory]
    [InlineData(GestureGrade.Perfect, 3f)]
    [InlineData(GestureGrade.Good, 2f)]
    [InlineData(GestureGrade.Failed, 1f)]
    public void GradeValues_PickByGrade(GestureGrade grade, float expected)
    {
        Assert.Equal(expected, new GradeValues(Perfect: 3f, Good: 2f, Failed: 1f).For(grade));
    }

    [Fact]
    public void Data_ReadsResolveDamage()
    {
        var damage = CombatData.ResolveDamageFromJson(
            """{ "resolve_damage": { "perfect": { "_tbd": true, "value": 30 }, "good": 18, "failed": 6 } }""");

        Assert.Equal(new GradeValues(30f, 18f, 6f), damage);
    }

    [Fact]
    public void Data_ReadsShadeResolve()
    {
        Assert.Equal(100f, CombatData.ShadeResolveFromJson("""{ "resolve": { "_tbd": true, "value": 100 } }"""));
    }
}
