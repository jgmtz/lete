using Lete.Core.Combat;
using Lete.Core.Events;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Tests;

public class ProfileTests
{
    public static readonly ProfileRules Rules = new(RecentWindow: 4, MinEncounters: 2, MinMemoryUses: 2);

    private readonly WorldState _state = new();
    private readonly EventBus _bus = new();
    private readonly Profile _profile;

    public ProfileTests() => _profile = new Profile(_state, _bus, Rules);

    private void Use(RecollectionKind kind) => _bus.Publish(new MemoryUsed("m", kind));

    [Fact]
    public void EachEncounterCountsTheViaThatEndedIt()
    {
        _bus.Publish(new ShadeFreed("a"));
        _bus.Publish(new ShadeAbsorbed("b"));
        _bus.Publish(new ShadeFreed("c"));
        _bus.Publish(new PlayerDefeated("d"));

        Assert.Equal(2, _profile.ViaTotals[Via.Free]);
        Assert.Equal(1, _profile.ViaTotals[Via.Absorb]);
        Assert.Equal(4, _state.Profile.Recent.Count);
        Assert.Null(_state.Profile.Recent[^1].Via);
        Assert.Equal(Via.Free, _profile.TopVia);
    }

    [Fact]
    public void Neutral_WithTooFewEncountersOrATie()
    {
        _bus.Publish(new ShadeFreed("a"));
        Assert.Null(_profile.TopVia);

        _bus.Publish(new ShadeDestroyed("b"));
        Assert.Null(_profile.TopVia);

        _bus.Publish(new ShadeDestroyed("c"));
        Assert.Equal(Via.Fight, _profile.TopVia);
    }

    [Fact]
    public void Recent_IsTheLastWindow_TotalsKeepEverything()
    {
        foreach (var shade in new[] { "a", "b", "c" })
        {
            _bus.Publish(new ShadeDestroyed(shade));
        }

        foreach (var shade in new[] { "d", "e", "f", "g" })
        {
            _bus.Publish(new ShadeSpared(shade));
        }

        Assert.Equal(Via.Spare, _profile.TopVia);
        Assert.Equal(4, _state.Profile.Recent.Count);
        Assert.Equal(3, _profile.ViaTotals[Via.Fight]);
    }

    [Fact]
    public void TopKind_ComesFromMemoriesUsedInTheRecentEncounters()
    {
        Use(RecollectionKind.Mourning);
        Use(RecollectionKind.Mourning);
        Use(RecollectionKind.Feast);
        Assert.Null(_profile.TopKind); // ainda não encerrou o encontro

        _bus.Publish(new ShadeFreed("a"));
        Assert.Equal(RecollectionKind.Mourning, _profile.TopKind);
        Assert.Equal(2, _profile.KindTotals[RecollectionKind.Mourning]);

        Use(RecollectionKind.Feast);
        _bus.Publish(new PlayerDefeated("b")); // a derrota não é via, mas as Memórias usadas contam
        Assert.Null(_profile.TopKind);
    }
}
