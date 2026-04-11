# BlueBeard.Events

Typed event bus with `[Flags]` enum masking for efficient subscriber filtering. Publish and subscribe to domain events without tightly coupling plugin components.

## Features

- Generic `EventBus<TAction>` scoped to a single enum type
- Bitmask-based subscription — register interest in `A | B | C` and receive any of those actions
- `EventContext<TAction>` carries player, UTC timestamp, arbitrary payload, and an advisory `Cancelled` flag
- Snapshot-on-publish iteration — subscribers can add/remove during dispatch without breaking the loop
- `EventBusManager` registry — one bus per enum type, shared across the plugin

## Quick example

```csharp
[Flags]
public enum FactionAction
{
    None       = 0,
    MemberJoined = 1 << 0,
    MemberLeft   = 1 << 1,
    RankChanged  = 1 << 2,
}

var bus = eventBusManager.GetOrCreate<FactionAction>();

bus.Subscribe(FactionAction.MemberJoined | FactionAction.MemberLeft, (action, ctx) =>
{
    var player = ctx.Player;
    UnturnedChat.Say(player, $"Faction event: {action}");
});

bus.Publish(FactionAction.MemberJoined, new EventContext<FactionAction>
{
    Player = somePlayer,
    Data = { ["faction"] = "Raiders" }
});
```

See `docs/Events/` in the Infrastructure repo for full reference.
