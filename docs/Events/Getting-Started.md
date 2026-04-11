# Getting Started

## Installation

Add a project reference to `BlueBeard.Events` in your plugin's `.csproj`:

```xml
<ProjectReference Include="..\BlueBeard.Events\BlueBeard.Events.csproj" />
```

BlueBeard.Events depends on `BlueBeard.Core` (pulled in automatically).

## Core Concepts

### Action enum
Every bus is scoped to a single `[Flags]` enum type. The flags should be power-of-two values so bitmask filtering works correctly.

```csharp
[Flags]
public enum FactionAction
{
    None         = 0,
    MemberJoined = 1 << 0,
    MemberLeft   = 1 << 1,
    RankChanged  = 1 << 2,
    Disbanded    = 1 << 3,
}
```

Non-flags enums technically work, but mask filtering degenerates to equality. Always use `[Flags]`.

### Event bus
`EventBus<FactionAction>` is the channel that carries all `FactionAction` events. Subscribers register a bitmask and a handler; publishers raise a single action at a time.

### Event context
`EventContext<FactionAction>` is the payload carried with every event. Its base fields (`Action`, `Player`, `Timestamp`, `Data`, `Cancelled`) are available to every subscriber.

## Basic Setup

```csharp
using BlueBeard.Events;

public class MyPlugin : RocketPlugin
{
    public static EventBusManager EventBuses { get; private set; }

    protected override void Load()
    {
        EventBuses = new EventBusManager();
        EventBuses.Load();

        var bus = EventBuses.GetOrCreate<FactionAction>();

        // Subscribe to two actions with one handler:
        bus.Subscribe(FactionAction.MemberJoined | FactionAction.MemberLeft, (action, ctx) =>
        {
            var faction = (string)ctx.Data["faction"];
            UnturnedChat.Say($"{ctx.Player.name} {action} faction {faction}");
        });
    }

    protected override void Unload()
    {
        EventBuses.Unload();
    }
}
```

## Publishing an Event

```csharp
var bus = MyPlugin.EventBuses.GetOrCreate<FactionAction>();

var ctx = new EventContext<FactionAction>
{
    Player = unturnedPlayer.Player,
    Data = { ["faction"] = "Raiders", ["previousRank"] = "Member", ["newRank"] = "Officer" }
};

bus.Publish(FactionAction.RankChanged, ctx);

if (ctx.Cancelled)
{
    // A subscriber vetoed the action; roll back.
    return;
}
```

## Unsubscribing

`Subscribe` returns a `Subscription` handle. Pass it to `Unsubscribe` to remove the handler:

```csharp
var sub = bus.Subscribe(FactionAction.Disbanded, OnDisbanded);
// ... later
bus.Unsubscribe(sub);
```

## Quick Reference

| Method | Purpose |
|--------|---------|
| `EventBusManager.GetOrCreate<T>()` | Resolve or lazily construct the bus for enum `T` |
| `EventBus<T>.Subscribe(mask, handler)` | Register a handler for any action matching `mask` |
| `EventBus<T>.Unsubscribe(subscription)` | Remove a previously registered handler |
| `EventBus<T>.Publish(action, context)` | Fire a single action to all matching subscribers |
| `EventBus<T>.Clear()` | Remove every subscriber from this bus |
| `EventBus<T>.SubscriberCount` | Diagnostics -- how many handlers are currently registered |
