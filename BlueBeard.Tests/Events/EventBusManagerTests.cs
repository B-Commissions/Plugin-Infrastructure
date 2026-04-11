using System;
using BlueBeard.Events;
using Xunit;

namespace BlueBeard.Tests.Events;

public class EventBusManagerTests
{
    [Flags]
    private enum ActionA { None = 0, X = 1 }

    [Flags]
    private enum ActionB { None = 0, Y = 1 }

    [Fact]
    public void GetOrCreate_Returns_Same_Instance_On_Repeated_Calls()
    {
        var mgr = new EventBusManager();
        var first = mgr.GetOrCreate<ActionA>();
        var second = mgr.GetOrCreate<ActionA>();
        Assert.Same(first, second);
    }

    [Fact]
    public void GetOrCreate_Returns_Different_Buses_For_Different_Enum_Types()
    {
        var mgr = new EventBusManager();
        var aBus = mgr.GetOrCreate<ActionA>();
        var bBus = mgr.GetOrCreate<ActionB>();
        Assert.NotSame((object)aBus, bBus);
    }

    [Fact]
    public void Unload_Clears_All_Buses()
    {
        var mgr = new EventBusManager();
        var bus = mgr.GetOrCreate<ActionA>();
        bus.Subscribe(ActionA.X, (_, _) => { });
        Assert.Equal(1, bus.SubscriberCount);

        mgr.Unload();

        // The previously-issued bus reference is cleared in place.
        Assert.Equal(0, bus.SubscriberCount);
    }
}
