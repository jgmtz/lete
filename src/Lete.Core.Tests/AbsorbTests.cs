using Lete.Core.Combat;
using Lete.Core.Data;

namespace Lete.Core.Tests;

public class AbsorbTests
{
    private static readonly EchoDefinition Remada = new("eco_remada", "n", "d", "eco_remada", new GradeValues(45f, 30f, 12f), 1);

    private static readonly AbsorbRules Rules = new(0.3f, 0.6f, new GradeValues(1f, 1f, 2f), Remada);

    [Fact]
    public void Absorb_OnlyWithLowResolve()
    {
        var resolve = new ShadeResolve(100f);
        Assert.False(Rules.CanAbsorb(resolve));

        resolve.Reduce(70f);
        Assert.True(Rules.CanAbsorb(resolve));

        resolve.Reduce(30f);
        Assert.False(Rules.CanAbsorb(resolve)); // já desfeita: não há o que absorver
    }

    [Fact]
    public void Absorb_FillsFasterTheWeakerTheShade()
    {
        Assert.Equal(1f, Rules.FillFactor(0.3f), 3);
        Assert.Equal(0.8f, Rules.FillFactor(0.15f), 3);
        Assert.Equal(0.6f, Rules.FillFactor(0f), 3);
    }

    [Fact]
    public void Echoes_HaveThreeSlots_OneUsePerBattle()
    {
        var slots = new EchoSlots();
        Assert.True(slots.Add(Remada));
        Assert.False(slots.Add(Remada)); // o mesmo Eco não ocupa dois espaços
        Assert.True(slots.Add(Remada with { Id = "b" }));
        Assert.True(slots.Add(Remada with { Id = "c" }));
        Assert.True(slots.IsFull);
        Assert.False(slots.Add(Remada with { Id = "d" }));

        Assert.True(slots.CanUse("eco_remada"));
        slots.Use("eco_remada");
        Assert.False(slots.CanUse("eco_remada"));
        Assert.Throws<InvalidOperationException>(() => slots.Use("eco_remada"));

        slots.NewBattle();
        Assert.True(slots.CanUse("eco_remada"));
    }

    [Fact]
    public void Flow_AbsorbedShadeEndsInVictoryAbsorbed()
    {
        var attacks = new[] { new ShadeAttack(ShadeAttackKind.Direct, "aparo", "golpe", new GradeValues(0f, 10f, 20f)) };
        var flow = new BattleFlow(new Gauge(100f), new Gauge(60f), new ShadeResolve(100f), attacks, null);
        flow.AbsorbShade();
        flow.EndPlayerAction();
        Assert.Equal(BattleOutcome.Absorbed, flow.Outcome);
    }

    [Fact]
    public void Data_LoadsTheRowerEcho()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "project.godot")))
        {
            dir = dir.Parent;
        }

        var root = Path.Combine(dir!.FullName, "content");
        var rules = ShadeData.AbsorbFromJson(File.ReadAllText(Path.Combine(root, "shades", "remador.json")));
        Assert.Equal("eco_remada", rules.Echo.Id);

        var gesture = File.ReadAllText(Path.Combine(root, "gestures", rules.Echo.Gesture + ".json"));
        Assert.Equal(2, GestureData.RingRepeats(gesture));
        Assert.Equal(1, GestureData.RingRepeats(File.ReadAllText(Path.Combine(root, "gestures", "anel_basico.json"))));
    }
}
