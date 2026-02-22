# Events

BlueBeard.Zones fires events when players enter or exit zones. These are the primary integration point for other plugins.

## ZoneManager Events

### PlayerEnteredZone

Fired when a player (or a player inside a vehicle) enters a zone's trigger collider.

```csharp
zoneManager.PlayerEnteredZone += (Player player, ZoneDefinition zone) =>
{
    var name = player.channel.owner.playerID.playerName;
    Logger.Log($"{name} entered zone {zone.Id}");
};
```

### PlayerExitedZone

Fired when a player (or a player inside a vehicle) exits a zone's trigger collider.

```csharp
zoneManager.PlayerExitedZone += (Player player, ZoneDefinition zone) =>
{
    var name = player.channel.owner.playerID.playerName;
    Logger.Log($"{name} left zone {zone.Id}");
};
```

## Event Signature

Both events use:
```csharp
Action<Player, ZoneDefinition>
```

Where:
- `Player` is `SDG.Unturned.Player` (the Unturned player object)
- `ZoneDefinition` is the full zone definition including ID, flags, metadata, etc.

## Vehicle Detection

The `ZoneComponent` handles vehicles automatically. When a vehicle enters or exits a zone trigger, events fire **once per passenger**. Each passenger receives their own `PlayerEnteredZone` / `PlayerExitedZone` event.

## Event Order

When a zone is created and flag enforcement is enabled, multiple systems subscribe to these events:

1. **PlayerTracker** -- updates its internal maps (player-to-zones, zone-to-players)
2. **Flag handlers** -- enforce flags (access, notifications, effects, groups, item equip)
3. **Your plugin** -- any custom handlers you've registered

All handlers run on the Unity main thread.

## Subscribing in Your Plugin

```csharp
protected override void Load()
{
    ZonesPlugin.Instance.ZoneManager.PlayerEnteredZone += OnPlayerEnteredZone;
    ZonesPlugin.Instance.ZoneManager.PlayerExitedZone += OnPlayerExitedZone;
}

protected override void Unload()
{
    // Always unsubscribe to prevent memory leaks
    if (ZonesPlugin.Instance?.ZoneManager != null)
    {
        ZonesPlugin.Instance.ZoneManager.PlayerEnteredZone -= OnPlayerEnteredZone;
        ZonesPlugin.Instance.ZoneManager.PlayerExitedZone -= OnPlayerExitedZone;
    }
}

private void OnPlayerEnteredZone(Player player, ZoneDefinition zone)
{
    // Your logic here
}

private void OnPlayerExitedZone(Player player, ZoneDefinition zone)
{
    // Your logic here
}
```

## Filtering by Zone

The events fire for **all** zones. Filter by zone ID or metadata to handle only your zones:

```csharp
private void OnPlayerEnteredZone(Player player, ZoneDefinition zone)
{
    // Only handle zones owned by your plugin
    if (zone.Metadata?.TryGetValue("owner", out var owner) != true || owner != "MyPlugin")
        return;

    // Only handle zones with a specific flag
    if (!zone.Flags.ContainsKey("myCustomFlag"))
        return;

    // Your logic
}
```
