using System;
using System.Collections.Generic;
using BlueBeard.Events;
using Xunit;

namespace BlueBeard.Tests.Events;

public class EventBusEnhancementTests
{
    [Flags]
    private enum Act { None = 0, Fire = 1, Ice = 2 }

    [Fact]
    public void Disposing_Subscription_Unsubscribes()
    {
        var bus = new EventBus<Act>();
        var hits = 0;

        using (bus.Subscribe(Act.Fire, (_, _) => hits++))
        {
            bus.Publish(Act.Fire, new EventContext<Act>());
        }

        bus.Publish(Act.Fire, new EventContext<Act>());

        Assert.Equal(1, hits);
        Assert.Equal(0, bus.SubscriberCount);
    }

    [Fact]
    public void Double_Dispose_Is_Safe()
    {
        var bus = new EventBus<Act>();
        var sub = bus.Subscribe(Act.Fire, (_, _) => { });
        sub.Dispose();
        sub.Dispose();
        Assert.Equal(0, bus.SubscriberCount);
    }

    [Fact]
    public void Higher_Priority_Handlers_Run_First()
    {
        var bus = new EventBus<Act>();
        var order = new List<string>();

        bus.Subscribe(Act.Fire, (_, _) => order.Add("default"));           // priority 0
        bus.Subscribe(Act.Fire, (_, _) => order.Add("high"), priority: 10);
        bus.Subscribe(Act.Fire, (_, _) => order.Add("low"), priority: -5);
        bus.Subscribe(Act.Fire, (_, _) => order.Add("high2"), priority: 10);

        bus.Publish(Act.Fire, new EventContext<Act>());

        Assert.Equal(["high", "high2", "default", "low"], order);
    }

    [Fact]
    public void PublishCancelable_Reports_Subscriber_Veto()
    {
        var bus = new EventBus<Act>();
        bus.Subscribe(Act.Fire, (_, ctx) => ctx.Cancelled = true);

        Assert.True(bus.PublishCancelable(Act.Fire, new EventContext<Act>()));
        Assert.False(bus.PublishCancelable(Act.Ice, new EventContext<Act>()));
    }

    [Fact]
    public void High_Priority_Veto_Reaches_Later_Handlers_As_Context_State()
    {
        var bus = new EventBus<Act>();
        var sawCancelled = false;

        bus.Subscribe(Act.Fire, (_, ctx) => ctx.Cancelled = true, priority: 10);
        bus.Subscribe(Act.Fire, (_, ctx) => sawCancelled = ctx.Cancelled, priority: 0);

        bus.PublishCancelable(Act.Fire, new EventContext<Act>());

        Assert.True(sawCancelled);
    }
}
