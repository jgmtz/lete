using Lete.Core.Events;

namespace Lete.Core.Tests;

public class EventBusTests
{
    private sealed record Ping(int Value);
    private sealed record Other;

    [Fact]
    public void Publish_DeliversToAllSubscribersOfThatType()
    {
        var bus = new EventBus();
        var received = new List<int>();
        bus.Subscribe<Ping>(p => received.Add(p.Value));
        bus.Subscribe<Ping>(p => received.Add(p.Value * 10));

        bus.Publish(new Ping(1));

        Assert.Equal(new[] { 1, 10 }, received);
    }

    [Fact]
    public void Publish_IgnoresSubscribersOfOtherTypes()
    {
        var bus = new EventBus();
        var called = false;
        bus.Subscribe<Other>(_ => called = true);

        bus.Publish(new Ping(1));

        Assert.False(called);
    }

    [Fact]
    public void Publish_WithoutSubscribers_DoesNothing()
    {
        var bus = new EventBus();

        var exception = Record.Exception(() => bus.Publish(new Ping(1)));

        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_StopsDelivery()
    {
        var bus = new EventBus();
        var count = 0;
        var subscription = bus.Subscribe<Ping>(_ => count++);

        bus.Publish(new Ping(1));
        subscription.Dispose();
        bus.Publish(new Ping(2));

        Assert.Equal(1, count);
    }

    [Fact]
    public void Subscriber_CanUnsubscribeWhilePublishing()
    {
        var bus = new EventBus();
        var secondCalled = false;
        IDisposable? first = null;
        first = bus.Subscribe<Ping>(_ => first!.Dispose());
        bus.Subscribe<Ping>(_ => secondCalled = true);

        bus.Publish(new Ping(1));

        Assert.True(secondCalled);
    }
}
