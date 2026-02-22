# BlueBeard.Holograms

A proximity-based hologram system for Unturned. Place 3D holograms in the world that automatically show/hide UI overlays when players walk near them. Supports pooling, per-player state, global allocation, and dynamic metadata updates.

## Installation

Add a project reference:

```xml
<ProjectReference Include="..\BlueBeard.Holograms\BlueBeard.Holograms.csproj" />
```

## Concepts

- **HologramDefinition** -- A world position + trigger radius. Defines _where_ a hologram exists.
- **Hologram** -- A pooled UI/Effect pair. The `Effect` is the 3D visual; the `UI` is the screen overlay.
- **IHologramDisplay** -- Your code that populates the UI when a player enters the zone.
- **HologramRegistration** -- Bundles definitions, pool, and display together for batch registration.

## Setup

```csharp
using BlueBeard.Holograms;

var hologramManager = new HologramManager();
hologramManager.Load();

// On unload:
hologramManager.Unload();
```

## Defining Holograms

### 1. Create the Pool

Each hologram in the pool is an Effect/UI pair from your Unity asset bundle:

```csharp
var pool = new List<Hologram>
{
    new() { UI = 50600, Effect = 50601 },
    new() { UI = 50602, Effect = 50603 },
    new() { UI = 50604, Effect = 50605 },
};
```

Pool size determines how many holograms can be visible simultaneously. With `IsGlobal = true`, each pool slot is shared across all players. With `IsGlobal = false`, each player gets their own allocation.

### 2. Implement IHologramDisplay

This controls what the player sees when they enter the hologram zone:

```csharp
using BlueBeard.Holograms;
using SDG.NetTransport;
using SDG.Unturned;

public class ShopHologramDisplay : IHologramDisplay
{
    public void Show(ITransportConnection connection, short key,
        HologramDefinition definition, Dictionary<string, string> metadata)
    {
        EffectManager.sendUIEffectVisibility(key, connection, true, "Canvas/ShopPanel", true);
        EffectManager.sendUIEffectText(key, connection, true, "Canvas/ShopPanel/Title",
            metadata.GetValueOrDefault("title", "Shop"));
    }

    public void Hide(ITransportConnection connection, short key, HologramDefinition definition)
    {
        EffectManager.sendUIEffectVisibility(key, connection, true, "Canvas/ShopPanel", false);
    }
}
```

### 3. Create Definitions and Register

```csharp
var definition = new HologramDefinition
{
    Position = new Vector3(100, 50, 200),
    Radius = 10f,
    Height = 20f,
    Metadata = new Dictionary<string, string>
    {
        { "title", "Weapon Shop" }
    }
};

hologramManager.RegisterDefinition(definition, new ShopHologramDisplay(), pool, isGlobal: true);
```

### Batch Registration

```csharp
var registration = new HologramRegistration
{
    Display = new ShopHologramDisplay(),
    Holograms = pool,
    Definitions = new List<HologramDefinition> { def1, def2, def3 },
    IsGlobal = true
};

hologramManager.Register(registration);
```

## Updating Holograms

### Update One Player

```csharp
hologramManager.UpdatePlayer(player, definition, new Dictionary<string, string>
{
    { "stock", "42" },
    { "discount", "20%" }
});
```

### Update All Players

```csharp
hologramManager.UpdateAll(definition, new Dictionary<string, string>
{
    { "stock", "0" },
    { "status", "SOLD OUT" }
});
```

## Events

```csharp
hologramManager.PlayerEnteredHologram += (player, definition) =>
{
    Logger.Log($"{player.channel.owner.playerID.playerName} entered hologram zone");
};

hologramManager.PlayerExitedHologram += (player, definition) =>
{
    Logger.Log($"{player.channel.owner.playerID.playerName} left hologram zone");
};
```

## Unregistering

```csharp
hologramManager.UnregisterDefinition(definition);
// Hides UI for all players, clears the effect, destroys the trigger zone
```

## Global vs Per-Player Pools

| Mode | `IsGlobal` | Behavior |
|------|------------|----------|
| Global | `true` | Pool slots are shared. If slot 0 is used by any player, no other player gets slot 0. Good for unique world objects. |
| Per-Player | `false` | Each player gets their own allocation from the pool. Good for instanced content. |

## Player Filtering

```csharp
hologramManager.RegisterDefinition(definition, display, pool, isGlobal: false,
    playerFilter: player => player.life.health > 50);
// Only players with >50 health will see this hologram
```
