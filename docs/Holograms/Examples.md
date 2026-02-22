# Examples

Complete implementation examples for common BlueBeard.Holograms use cases.

## Shop Hologram with Item Name and Price

A basic shop hologram that displays an item name and price from metadata.

### Display Implementation

```csharp
using System.Collections.Generic;
using BlueBeard.Holograms;
using SDG.NetTransport;
using SDG.Unturned;

public class ShopHologramDisplay : IHologramDisplay
{
    public void Show(ITransportConnection connection, short key,
        HologramDefinition definition, Dictionary<string, string> metadata)
    {
        EffectManager.sendUIEffectVisibility(key, connection, true, "Canvas/ShopPanel", true);

        var itemName = metadata.GetValueOrDefault("item_name", "Unknown Item");
        var price = metadata.GetValueOrDefault("price", "$0");

        EffectManager.sendUIEffectText(key, connection, true,
            "Canvas/ShopPanel/ItemName", itemName);
        EffectManager.sendUIEffectText(key, connection, true,
            "Canvas/ShopPanel/Price", price);
    }

    public void Hide(ITransportConnection connection, short key,
        HologramDefinition definition)
    {
        EffectManager.sendUIEffectVisibility(key, connection, true, "Canvas/ShopPanel", false);
    }
}
```

### Registration

```csharp
using BlueBeard.Holograms;
using UnityEngine;

// Create the pool -- 3 slots means up to 3 shops visible at once (globally)
var pool = new List<Hologram>
{
    new() { UI = 50600, Effect = 50601 },
    new() { UI = 50602, Effect = 50603 },
    new() { UI = 50604, Effect = 50605 },
};

var display = new ShopHologramDisplay();

// Define individual shop locations
var weaponShop = new HologramDefinition
{
    Position = new Vector3(100, 50, 200),
    Radius = 8f,
    Height = 16f,
    Metadata = new Dictionary<string, string>
    {
        { "item_name", "Assault Rifle" },
        { "price", "$500" }
    }
};

var armorShop = new HologramDefinition
{
    Position = new Vector3(150, 50, 220),
    Radius = 8f,
    Height = 16f,
    Metadata = new Dictionary<string, string>
    {
        { "item_name", "Kevlar Vest" },
        { "price", "$300" }
    }
};

// Register both definitions sharing the same pool and display
hologramManager.RegisterDefinition(weaponShop, display, pool, isGlobal: true);
hologramManager.RegisterDefinition(armorShop, display, pool, isGlobal: true);
```

---

## Hologram with Dynamic Stock Count Updates

Extends the shop example by updating stock counts at runtime whenever a purchase occurs.

### Tracking Stock and Updating

```csharp
// Stock tracker
var stockCounts = new Dictionary<HologramDefinition, int>
{
    { weaponShop, 10 },
    { armorShop, 25 }
};

// Call this when a player purchases an item
void OnItemPurchased(HologramDefinition shopDefinition)
{
    if (!stockCounts.ContainsKey(shopDefinition)) return;

    stockCounts[shopDefinition]--;
    var remaining = stockCounts[shopDefinition];

    // Update all players currently viewing this hologram
    hologramManager.UpdateAll(shopDefinition, new Dictionary<string, string>
    {
        { "stock", remaining.ToString() },
        { "status", remaining > 0 ? "In Stock" : "SOLD OUT" }
    });
}
```

### Updated Display

```csharp
public class ShopWithStockDisplay : IHologramDisplay
{
    public void Show(ITransportConnection connection, short key,
        HologramDefinition definition, Dictionary<string, string> metadata)
    {
        EffectManager.sendUIEffectVisibility(key, connection, true, "Canvas/ShopPanel", true);

        EffectManager.sendUIEffectText(key, connection, true,
            "Canvas/ShopPanel/ItemName",
            metadata.GetValueOrDefault("item_name", "Unknown"));

        EffectManager.sendUIEffectText(key, connection, true,
            "Canvas/ShopPanel/Price",
            metadata.GetValueOrDefault("price", "$0"));

        var stock = metadata.GetValueOrDefault("stock", "?");
        var status = metadata.GetValueOrDefault("status", "In Stock");

        EffectManager.sendUIEffectText(key, connection, true,
            "Canvas/ShopPanel/Stock", $"Stock: {stock}");

        EffectManager.sendUIEffectText(key, connection, true,
            "Canvas/ShopPanel/Status", status);
    }

    public void Hide(ITransportConnection connection, short key,
        HologramDefinition definition)
    {
        EffectManager.sendUIEffectVisibility(key, connection, true, "Canvas/ShopPanel", false);
    }
}
```

---

## Per-Player Instanced Hologram (Personal Stats Display)

Each player sees their own stats when entering the zone. Per-player mode ensures all players can view the hologram simultaneously without conflicting pool slots.

### Display Implementation

```csharp
public class PlayerStatsDisplay : IHologramDisplay
{
    public void Show(ITransportConnection connection, short key,
        HologramDefinition definition, Dictionary<string, string> metadata)
    {
        EffectManager.sendUIEffectVisibility(key, connection, true, "Canvas/StatsPanel", true);

        EffectManager.sendUIEffectText(key, connection, true,
            "Canvas/StatsPanel/Kills",
            $"Kills: {metadata.GetValueOrDefault("kills", "0")}");

        EffectManager.sendUIEffectText(key, connection, true,
            "Canvas/StatsPanel/Deaths",
            $"Deaths: {metadata.GetValueOrDefault("deaths", "0")}");

        EffectManager.sendUIEffectText(key, connection, true,
            "Canvas/StatsPanel/Score",
            $"Score: {metadata.GetValueOrDefault("score", "0")}");
    }

    public void Hide(ITransportConnection connection, short key,
        HologramDefinition definition)
    {
        EffectManager.sendUIEffectVisibility(key, connection, true, "Canvas/StatsPanel", false);
    }
}
```

### Registration and Per-Player Updates

```csharp
// Per-player pool -- each player gets their own slot allocation
var statsPool = new List<Hologram>
{
    new() { UI = 51000, Effect = 51001 },
};

var statsDisplay = new PlayerStatsDisplay();

var statsBoard = new HologramDefinition
{
    Position = new Vector3(0, 50, 0),
    Radius = 5f,
    Height = 10f,
    Metadata = new Dictionary<string, string>
    {
        { "kills", "0" },
        { "deaths", "0" },
        { "score", "0" }
    }
};

// Per-player mode: each player gets independent slot allocation
hologramManager.RegisterDefinition(statsBoard, statsDisplay, statsPool, isGlobal: false);

// When a player enters, populate with their personal data
hologramManager.PlayerEnteredHologram += (player, definition) =>
{
    if (definition != statsBoard) return;

    var playerData = GetPlayerStats(player); // your stats lookup

    hologramManager.UpdatePlayer(player, definition, new Dictionary<string, string>
    {
        { "kills", playerData.Kills.ToString() },
        { "deaths", playerData.Deaths.ToString() },
        { "score", playerData.Score.ToString() }
    });
};
```

Because `IsGlobal` is `false`, every player entering the zone gets slot 0 from their own tracker. Ten players can stand in the zone simultaneously, each seeing their own stats.

---

## Player Filtering: Group-Restricted Holograms

Show holograms only to players who meet specific criteria using the `playerFilter` parameter.

### Admin-Only Hologram

```csharp
var adminPanel = new HologramDefinition
{
    Position = new Vector3(50, 50, 50),
    Radius = 5f,
    Height = 10f,
    Metadata = new Dictionary<string, string>
    {
        { "title", "Admin Control Panel" }
    }
};

hologramManager.RegisterDefinition(adminPanel, new AdminDisplay(), pool, isGlobal: false,
    playerFilter: player => player.channel.owner.isAdmin);
```

Only players with admin privileges will see this hologram. Non-admin players can walk through the zone without triggering anything.

### Permission-Based Filtering

```csharp
// Only VIP players see this hologram
hologramManager.RegisterDefinition(vipShop, display, pool, isGlobal: false,
    playerFilter: player =>
    {
        var rocketPlayer = UnturnedPlayer.FromPlayer(player);
        return rocketPlayer.HasPermission("vip");
    });
```

### Group-Based Filtering

```csharp
// Only players in a specific Steam group
var allowedGroupId = new CSteamID(123456789);

hologramManager.RegisterDefinition(groupHQ, display, pool, isGlobal: false,
    playerFilter: player =>
        player.quests.groupID == allowedGroupId);
```

### Health-Based Filtering

```csharp
// Only show a healing station hologram to injured players
hologramManager.RegisterDefinition(healStation, display, pool, isGlobal: false,
    playerFilter: player => player.life.health < 100);
```

Note that the filter is evaluated **once** when the player enters the trigger zone. If a player's health changes while inside the zone, the hologram remains visible (or invisible) until they exit and re-enter.

---

## Combining Patterns: Full Plugin Example

A condensed example showing all pieces together in a plugin lifecycle.

```csharp
using System.Collections.Generic;
using BlueBeard.Holograms;
using Rocket.Core.Logging;
using SDG.Unturned;
using UnityEngine;

public class MyHologramPlugin
{
    private HologramManager _hologramManager;
    private HologramDefinition _shopDef;
    private HologramDefinition _statsDef;

    public void Load()
    {
        _hologramManager = new HologramManager();
        _hologramManager.Load();

        // -- Global shop hologram --
        var shopPool = new List<Hologram>
        {
            new() { UI = 50600, Effect = 50601 },
            new() { UI = 50602, Effect = 50603 },
        };

        _shopDef = new HologramDefinition
        {
            Position = new Vector3(100, 50, 200),
            Radius = 10f,
            Height = 20f,
            Metadata = new Dictionary<string, string>
            {
                { "item_name", "Medkit" },
                { "price", "$150" },
                { "stock", "20" }
            }
        };

        _hologramManager.RegisterDefinition(_shopDef, new ShopWithStockDisplay(),
            shopPool, isGlobal: true);

        // -- Per-player stats hologram (admin-only) --
        var statsPool = new List<Hologram>
        {
            new() { UI = 51000, Effect = 51001 },
        };

        _statsDef = new HologramDefinition
        {
            Position = new Vector3(0, 50, 0),
            Radius = 5f,
            Height = 10f
        };

        _hologramManager.RegisterDefinition(_statsDef, new PlayerStatsDisplay(),
            statsPool, isGlobal: false,
            playerFilter: player => player.channel.owner.isAdmin);

        // -- Events --
        _hologramManager.PlayerEnteredHologram += OnEntered;
        _hologramManager.PlayerExitedHologram += OnExited;
    }

    public void Unload()
    {
        _hologramManager.PlayerEnteredHologram -= OnEntered;
        _hologramManager.PlayerExitedHologram -= OnExited;
        _hologramManager.Unload();
    }

    private void OnEntered(Player player, HologramDefinition definition)
    {
        Logger.Log($"{player.channel.owner.playerID.playerName} entered hologram");

        if (definition == _statsDef)
        {
            var stats = GetPlayerStats(player);
            _hologramManager.UpdatePlayer(player, definition, new Dictionary<string, string>
            {
                { "kills", stats.Kills.ToString() },
                { "deaths", stats.Deaths.ToString() },
                { "score", stats.Score.ToString() }
            });
        }
    }

    private void OnExited(Player player, HologramDefinition definition)
    {
        Logger.Log($"{player.channel.owner.playerID.playerName} exited hologram");
    }

    // Placeholder for your stats lookup
    private dynamic GetPlayerStats(Player player) =>
        new { Kills = 10, Deaths = 3, Score = 1500 };
}
```
