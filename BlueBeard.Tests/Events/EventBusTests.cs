using System;
using System.Collections.Generic;
using BlueBeard.Events;
using Xunit;

namespace BlueBeard.Tests.Events;

public class EventBusTests
{
    [Flags]
    private enum TestAction
    {
        None = 0,
        A = 1 << 0,
        B = 1 << 1,
        C = 1 << 2,
    }

    [Fact]
    public void Subscriber_With_Mask_A_Or_B_Receives_A()
    {
        var bus = new EventBus<TestAction>();
        var received = new List<TestAction>();
        bus.Subscribe(TestAction.A | TestAction.B, (action, _) => received.Add(action));

        bus.Publish(TestAction.A, new EventContext<TestAction>());

        Assert.Single(received);
        Assert.Equal(TestAction.A, received[0]);
    }

    [Fact]
    public void Subscriber_With_Mask_A_Or_B_Receives_B()
    {
        var bus = new EventBus<TestAction>();
        var received = new List<TestAction>();
        bus.Subscribe(TestAction.A | TestAction.B, (action, _) => received.Add(action));

        bus.Publish(TestAction.B, new EventContext<TestAction>());

        Assert.Single(received);
        Assert.Equal(TestAction.B, received[0]);
    }

    [Fact]
    public void Subscriber_With_Mask_A_Or_B_Does_Not_Receive_C()
    {
        var bus = new EventBus<TestAction>();
        var received = new List<TestAction>();
        bus.Subscribe(TestAction.A | TestAction.B, (action, _) => received.Add(action));

        bus.Publish(TestAction.C, new EventContext<TestAction>());

        Assert.Empty(received);
    }

    [Fact]
    public void Unsubscribe_Prevents_Further_Delivery()
    {
        var bus = new EventBus<TestAction>();
        var count = 0;
        var sub = bus.Subscribe(TestAction.A, (_, _) => count++);

        bus.Publish(TestAction.A, new EventContext<TestAction>());
        Assert.Equal(1, count);

        bus.Unsubscribe(sub);
        bus.Publish(TestAction.A, new EventContext<TestAction>());
        Assert.Equal(1, count);
    }

    [Fact]
    public void Clear_Removes_All_Subscriptions()
    {
        var bus = new EventBus<TestAction>();
        bus.Subscribe(TestAction.A, (_, _) => { });
        bus.Subscribe(TestAction.B, (_, _) => { });
        Assert.Equal(2, bus.SubscriberCount);

        bus.Clear();

        Assert.Equal(0, bus.SubscriberCount);
    }

    [Fact]
    public void Cancelled_Flag_Is_Readable_After_Dispatch()
    {
        var bus = new EventBus<TestAction>();
        bus.Subscribe(TestAction.A, (_, ctx) => ctx.Cancelled = true);

        var context = new EventContext<TestAction>();
        bus.Publish(TestAction.A, context);

        Assert.True(context.Cancelled);
    }

    [Fact]
    public void Multiple_Subscribers_With_Overlapping_Masks_All_Receive_Event()
    {
        var bus = new EventBus<TestAction>();
        var a = 0;
        var b = 0;
        var c = 0;
        bus.Subscribe(TestAction.A, (_, _) => a++);
        bus.Subscribe(TestAction.A | TestAction.B, (_, _) => b++);
        bus.Subscribe(TestAction.A | TestAction.C, (_, _) => c++);

        bus.Publish(TestAction.A, new EventContext<TestAction>());

        Assert.Equal(1, a);
        Assert.Equal(1, b);
        Assert.Equal(1, c);
    }

    [Fact]
    public void Subscribe_During_Publish_Does_Not_Throw()
    {
        var bus = new EventBus<TestAction>();
        bus.Subscribe(TestAction.A, (_, _) =>
        {
            bus.Subscribe(TestAction.A, (_, _) => { });
        });

        var ex = Record.Exception(() => bus.Publish(TestAction.A, new EventContext<TestAction>()));
        Assert.Null(ex);
    }

    [Fact]
    public void Unsubscribe_During_Publish_Does_Not_Throw()
    {
        var bus = new EventBus<TestAction>();
        Subscription sub = null;
        sub = bus.Subscribe(TestAction.A, (_, _) => bus.Unsubscribe(sub));

        var ex = Record.Exception(() => bus.Publish(TestAction.A, new EventContext<TestAction>()));
        Assert.Null(ex);
    }

    [Fact]
    public void Context_Action_Is_Set_To_Published_Action()
    {
        var bus = new EventBus<TestAction>();
        TestAction seen = TestAction.None;
        bus.Subscribe(TestAction.A, (_, ctx) => seen = ctx.Action);

        bus.Publish(TestAction.A, new EventContext<TestAction>());

        Assert.Equal(TestAction.A, seen);
    }
}
