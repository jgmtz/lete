using Lete.Core.Events;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Tests;

public class TideTests
{
    private readonly WorldState _state = new();
    private readonly EventBus _bus = new();
    private readonly List<int> _steps = new();
    private readonly List<TideAdvanced> _advances = new();
    private readonly TideTracker _tide;

    public TideTests()
    {
        _bus.Subscribe<TideStepReached>(e => _steps.Add(e.Step));
        _bus.Subscribe<TideAdvanced>(_advances.Add);
        _tide = new TideTracker(_state, _bus, new TideRules(MilestoneStep: 1m, TravelStep: 0.1m));
    }

    [Fact]
    public void Milestone_CountsOnce()
    {
        Assert.True(_tide.Milestone("aqueronte"));
        Assert.False(_tide.Milestone("aqueronte"));

        Assert.Equal(1m, _state.Tide);
        Assert.Equal(new[] { 1 }, _steps);
        Assert.Equal(new TideAdvanced(1m, TideCause.Milestone), Assert.Single(_advances));
    }

    [Fact]
    public void Travel_TenTripsMakeExactlyOneStep()
    {
        for (var i = 0; i < 9; i++)
        {
            _tide.Travel();
        }

        Assert.Equal(0, _tide.Step);
        Assert.Empty(_steps);

        _tide.Travel();
        Assert.Equal(1, _tide.Step);
        Assert.Equal(new[] { 1 }, _steps);
    }

    [Fact]
    public void Defeat_ByModeCanCrossTwoStepsAndZeroDoesNothing()
    {
        _tide.Travel();
        _tide.Defeat(0);
        Assert.Single(_advances);

        _tide.Defeat(2);
        Assert.Equal(2.1m, _state.Tide);
        Assert.Equal(new[] { 1, 2 }, _steps);
        Assert.Throws<ArgumentOutOfRangeException>(() => _tide.Defeat(-1));
    }
}
