# Event Contexts

## EventContext&lt;TAction&gt;

The payload passed with every event through an `EventBus<TAction>`. Always constructed by the publisher; read by every subscriber that matches the published action.

### Properties

| Property | Type | Notes |
|----------|------|-------|
| `Action` | `TAction` | The specific action that was raised. Set automatically by `Publish`; subscribers read it to distinguish which action fired when their mask matches multiple. |
| `Player` | `SDG.Unturned.Player` | The player involved in the event, if any. Null for non-player-initiated events. |
| `Timestamp` | `DateTime` | UTC time at which the context was constructed. Useful for ordering or idempotency checks. |
| `Data` | `Dictionary<string, object>` | Arbitrary key/value payload. Publisher populates it; subscribers read what they need. |
| `Cancelled` | `bool` | Advisory flag any subscriber can set. The publisher inspects it after `Publish` returns. |

### Populating the data bag

The `Data` dictionary is the extensibility point for domain-specific payload. Keep keys namespaced to avoid collisions across multiple publishers that share a single action enum:

```csharp
var ctx = new EventContext<FactionAction>
{
    Player = player,
    Data =
    {
        ["faction.id"] = factionId,
        ["faction.name"] = factionName,
        ["rank.previous"] = previousRank,
        ["rank.new"] = newRank,
    }
};
bus.Publish(FactionAction.RankChanged, ctx);
```

Subscribers cast as needed:

```csharp
bus.Subscribe(FactionAction.RankChanged, (_, ctx) =>
{
    var newRank = (string)ctx.Data["rank.new"];
    // ...
});
```

### The Cancelled flag

`Cancelled` is **advisory**. The bus never enforces it -- cancellation is purely a convention between the publisher and its subscribers. The typical pattern:

```csharp
var ctx = new EventContext<FactionAction>();
bus.Publish(FactionAction.MemberJoined, ctx);
if (ctx.Cancelled)
{
    // A subscriber objected (e.g. the player is on a blocklist). Roll back.
    RemoveFactionMember(playerId, factionId);
    return;
}
```

Any subscriber can set the flag:

```csharp
bus.Subscribe(FactionAction.MemberJoined, (_, ctx) =>
{
    if (_blocklist.Contains(ctx.Player.channel.owner.playerID.steamID.m_SteamID))
        ctx.Cancelled = true;
});
```

If multiple subscribers set `Cancelled`, the final value is still `true` -- the flag is one-way. To report *why* a subscriber cancelled, add an entry to `Data`:

```csharp
ctx.Cancelled = true;
ctx.Data["cancel.reason"] = "player_on_blocklist";
```

### Subclassing

For domain-specific contexts you can subclass `EventContext<TAction>` and add strongly-typed properties:

```csharp
public class FactionEventContext : EventContext<FactionAction>
{
    public int FactionId { get; set; }
    public string FactionName { get; set; }
    public string NewRank { get; set; }
}
```

The bus `Publish` signature accepts the base type, so subclasses work transparently.
