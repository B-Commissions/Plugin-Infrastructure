# Examples

> Examples below assume `RocketModBootstrap.Install()` (or `OpenModBootstrap.Install(...)`) has been called at plugin load — see [Mod-Host Adapters: Getting Started](../Mods/Getting-Started.md).

## Faction lifecycle events

Define the action enum and a strongly-typed context:

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

public class FactionContext : EventContext<FactionAction>
{
    public int FactionId { get; set; }
    public string FactionName { get; set; }
}
```

Subscribe once during plugin `Load`:

```csharp
var bus = EventBuses.GetOrCreate<FactionAction>();

bus.Subscribe(FactionAction.MemberJoined | FactionAction.MemberLeft, (action, ctx) =>
{
    var fctx = (FactionContext)ctx;
    var verb = action == FactionAction.MemberJoined ? "joined" : "left";
    BlueBeardHost.Chat.Broadcast($"{ctx.Player.name} {verb} faction {fctx.FactionName}");
});

bus.Subscribe(FactionAction.Disbanded, (_, ctx) =>
{
    var fctx = (FactionContext)ctx;
    BlueBeardHost.Logger.Log($"Faction {fctx.FactionId} disbanded at {ctx.Timestamp:u}");
});
```

Publish when the faction manager mutates state:

```csharp
public void JoinFaction(UnturnedPlayer player, int factionId)
{
    var faction = Lookup(factionId);
    _members.Add(player.CSteamID.m_SteamID, factionId);

    var bus = MyPlugin.EventBuses.GetOrCreate<FactionAction>();
    bus.Publish(FactionAction.MemberJoined, new FactionContext
    {
        Player = player.Player,
        FactionId = factionId,
        FactionName = faction.Name,
    });
}
```

## Cancellation pattern

Let external systems veto an action by setting `Cancelled`:

```csharp
[Flags]
public enum TradeAction
{
    None         = 0,
    OfferCreated = 1 << 0,
    OfferAccepted = 1 << 1,
    OfferCancelled = 1 << 2,
}

var bus = EventBuses.GetOrCreate<TradeAction>();

// An anti-cheat subscriber refuses trades from recently-flagged players:
bus.Subscribe(TradeAction.OfferCreated, (_, ctx) =>
{
    var steamId = ctx.Player.channel.owner.playerID.steamID.m_SteamID;
    if (_antiCheat.IsFlagged(steamId))
    {
        ctx.Cancelled = true;
        ctx.Data["cancel.reason"] = "antcheat_flag";
    }
});

// The trade manager inspects the flag:
public bool TryCreateOffer(UnturnedPlayer player, Offer offer)
{
    var ctx = new EventContext<TradeAction> { Player = player.Player };
    ctx.Data["offer.id"] = offer.Id;

    bus.Publish(TradeAction.OfferCreated, ctx);
    if (ctx.Cancelled)
    {
        BlueBeardHost.Chat.Say(player.CSteamID, $"Offer rejected ({ctx.Data["cancel.reason"]}).");
        return false;
    }

    _offers.Add(offer);
    return true;
}
```

## Cross-system communication without direct references

Two unrelated plugin subsystems communicate via a shared bus without holding references to each other:

```csharp
// ZoneSystem raises zone events:
var zoneBus = EventBuses.GetOrCreate<ZoneAction>();
zoneBus.Publish(ZoneAction.PlayerEntered, new EventContext<ZoneAction>
{
    Player = player,
    Data = { ["zone.id"] = zoneId, ["zone.name"] = zoneName }
});

// CurrencySystem hears the event and awards a per-zone tick bonus:
zoneBus.Subscribe(ZoneAction.PlayerEntered, (_, ctx) =>
{
    var zoneName = (string)ctx.Data["zone.name"];
    if (_zoneBonuses.TryGetValue(zoneName, out var bonus))
        AwardCurrency(ctx.Player, bonus);
});
```

Neither system references the other; the enum and the event bus manager are the only shared dependencies.
