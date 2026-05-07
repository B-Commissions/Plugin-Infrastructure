# Examples

## Storage crate with owner stamp (cursor API)

Goal: when a player equips a storage crate item, stamp the owner's Steam ID into the item state so we can later look up who owns it. Uses the cursor `StateWriter` so we don't track offsets manually.

```csharp
using BlueBeard.Items;

public class CrateBehaviour : ItemBehaviourBase
{
    public const ushort CrateAssetId = 10500;
    public const int StateSize = 10; // 8 bytes owner + 2 bytes spare

    public override void OnEquipped(Player player, ItemJar jar)
    {
        if (!ItemStateValidator.IsSafeForCustomState(jar.item.id)) return;

        var owner = player.channel.owner.playerID.steamID.m_SteamID;
        jar.item.state = new StateWriter(StateSize)
            .WriteUInt64(owner)
            .ToArray();
    }
}

// Registration:
MyPlugin.Items.Register(CrateBehaviour.CrateAssetId, new CrateBehaviour());
```

Read it back with the cursor reader:

```csharp
public ulong ReadCrateOwner(ItemJar jar)
{
    if (jar?.item?.state == null || jar.item.state.Length < 8) return 0;
    return new StateReader(jar.item.state).ReadUInt64();
}
```

Or use the offset-based static API for the same result:

```csharp
public ulong ReadCrateOwner(ItemJar jar)
{
    if (jar?.item?.state == null || jar.item.state.Length < 8) return 0;
    return ItemStateEncoder.ReadUInt64(jar.item.state, 0);
}
```

Retrieve the owner later:

```csharp
public ulong ReadCrateOwner(ItemJar jar)
{
    if (jar?.item?.state == null || jar.item.state.Length < 8) return 0;
    return ItemStateEncoder.ReadUInt64(jar.item.state, 0);
}
```

## Locked medkit (charges + unlock timestamp)

Goal: a medkit item with three charges that locks for 5 minutes between uses. The charges and unlock timestamp travel with the item in its state.

```csharp
public class LockedMedkitBehaviour : ItemBehaviourBase
{
    public const ushort AssetId = 12001;
    private const int StateSize = 13;
    // Layout:
    //   0..3   uint  chargesRemaining
    //   4..11  ulong unlockUnixSeconds
    //   12     bool  reserved

    public override void OnEquipped(Player player, ItemJar jar)
    {
        EnsureState(jar);
        var charges = ItemStateEncoder.ReadUInt32(jar.item.state, 0);
        var unlockAt = (long)ItemStateEncoder.ReadUInt64(jar.item.state, 4);
        var remaining = unlockAt - DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (remaining > 0)
            ChatManager.serverSendMessage($"Medkit locked for {remaining}s", Color.red, toPlayer: player.channel.owner);
        else
            ChatManager.serverSendMessage($"Medkit ready ({charges} charges)", Color.green, toPlayer: player.channel.owner);
    }

    // Called manually from the plugin's UseableConsumeable override:
    public void OnHeal(Player player, ItemJar jar)
    {
        EnsureState(jar);
        var charges = ItemStateEncoder.ReadUInt32(jar.item.state, 0);
        if (charges == 0) return;

        charges--;
        ItemStateEncoder.WriteUInt32(jar.item.state, 0, charges);
        ItemStateEncoder.WriteUInt64(jar.item.state, 4,
            (ulong)DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds());

        player.life.askHeal(50, true, true);
    }

    private static void EnsureState(ItemJar jar)
    {
        if (jar.item.state == null || jar.item.state.Length < StateSize)
            jar.item.state = new byte[StateSize];
    }
}
```

## Lockpick (veto pickup by non-owner)

Goal: a lockpick that can only be picked up by the player who dropped it.

```csharp
public class LockpickBehaviour : ItemBehaviourBase
{
    public override bool OnPickedUp(Player player, ItemJar jar)
    {
        if (jar?.item?.state == null || jar.item.state.Length < 8) return true;
        var owner = ItemStateEncoder.ReadUInt64(jar.item.state, 0);
        return owner == player.channel.owner.playerID.steamID.m_SteamID;
    }

    public override void OnDropped(Player player, ItemJar jar)
    {
        if (jar.item.state == null || jar.item.state.Length < 8)
            jar.item.state = new byte[8];
        ItemStateEncoder.WriteUInt64(jar.item.state, 0, player.channel.owner.playerID.steamID.m_SteamID);
    }
}
```

Enforce the veto from the calling site:

```csharp
public void OnPlayerPickupRequested(UnturnedPlayer player, ItemJar jar)
{
    if (!MyPlugin.Items.NotifyPickedUp(player.Player, jar))
    {
        // Block the pickup and return the item to the ground.
        BlueBeardHost.Chat.Say(player.CSteamID, "You are not the owner of that lockpick.");
        RejectPickup(player, jar);
    }
}
```

## Composite behaviour (multiple plugins extending the same item)

When two systems want to react to the same asset ID, compose handlers explicitly:

```csharp
public class CompositeBehaviour : ItemBehaviourBase
{
    private readonly List<IItemBehaviour> _handlers = new();
    public void Add(IItemBehaviour h) => _handlers.Add(h);

    public override void OnEquipped(Player p, ItemJar j)
    {
        foreach (var h in _handlers) h.OnEquipped(p, j);
    }
    public override void OnDequipped(Player p, ItemJar j)
    {
        foreach (var h in _handlers) h.OnDequipped(p, j);
    }
    public override void OnUsed(Player p, ItemJar j)
    {
        foreach (var h in _handlers) h.OnUsed(p, j);
    }
    public override void OnDropped(Player p, ItemJar j)
    {
        foreach (var h in _handlers) h.OnDropped(p, j);
    }
    public override bool OnPickedUp(Player p, ItemJar j)
    {
        foreach (var h in _handlers) if (!h.OnPickedUp(p, j)) return false;
        return true;
    }
}

var composite = new CompositeBehaviour();
composite.Add(new CrateBehaviour());
composite.Add(new AntiRaidBehaviour());
MyPlugin.Items.Register(CrateBehaviour.CrateAssetId, composite);
```

## Barricade — owner-only salvage

Goal: a storage barricade (asset 10500 once placed) can only be salvaged by its owner.

```csharp
using BlueBeard.Items.Behaviours;
using SDG.Unturned;
using Steamworks;

public class CrateOwnerOnlySalvage : BarricadeBehaviourBase
{
    public override bool OnSalvageRequested(BarricadeDrop drop, SteamPlayer instigator)
    {
        var owner = drop.GetServersideData().owner;
        return owner == 0 || owner == instigator.playerID.steamID.m_SteamID;
    }

    public override void OnSpawned(BarricadeDrop drop)
    {
        // Optional — log or seed extended state on spawn
    }
}

// Registration
MyPlugin.Barricades.Register(10500, new CrateOwnerOnlySalvage());
MyPlugin.Barricades.Load();
```

## Vehicle — VIP-only entry, no siphoning

```csharp
using BlueBeard.Items.Behaviours;
using SDG.Unturned;
using Steamworks;

public class VipVehicleBehaviour : VehicleBehaviourBase
{
    public override bool OnEnterRequested(Player player, InteractableVehicle vehicle)
    {
        var steamId = player.channel.owner.playerID.steamID.m_SteamID;
        // Only the locked owner may enter
        return vehicle.lockedOwner.m_SteamID == steamId;
    }

    public override bool OnSiphonRequested(InteractableVehicle vehicle, Player instigator, ushort desiredAmount)
    {
        // VIP vehicles can never be siphoned
        return false;
    }
}

MyPlugin.Vehicles.Register(95, new VipVehicleBehaviour());
MyPlugin.Vehicles.Load();
```

## Structure — protected build

```csharp
using BlueBeard.Items.Behaviours;
using SDG.Unturned;
using Steamworks;

public class IndestructibleStructure : StructureBehaviourBase
{
    public override bool OnDamageRequested(CSteamID instigator, StructureDrop drop, ushort pendingDamage, EDamageOrigin origin)
    {
        // Block all damage
        return false;
    }
}

MyPlugin.Structures.Register(2065, new IndestructibleStructure());
MyPlugin.Structures.Load();
```

## Animal — health-doubled boss deer

```csharp
using BlueBeard.Items.Behaviours;
using SDG.Unturned;

public class TankyDeer : AnimalBehaviourBase
{
    public override bool OnDamageRequested(ref DamageAnimalParameters parameters)
    {
        // Halve incoming damage
        parameters.damage *= 0.5f;
        return true;
    }
}

MyPlugin.Animals.Register(36, new TankyDeer());
MyPlugin.Animals.Load();
```

## Cursor + reader pair (multi-field item state)

Goal: a custom signal-flare item that records (owner Steam ID, deployed-at timestamp, paint colour, optional message) and stamps it into the item state on equip.

```csharp
using BlueBeard.Items;

public class FlareBehaviour : ItemBehaviourBase
{
    private const int MessageMax = 64; // 2 bytes length + up to 62 bytes UTF-8
    private const int StateSize = 8 + 8 + 4 + MessageMax;

    public override void OnEquipped(Player player, ItemJar jar)
    {
        if (!ItemStateValidator.IsSafeForCustomState(jar.item.id)) return;

        var owner = player.channel.owner.playerID.steamID.m_SteamID;
        var deployedAt = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var paintArgb = 0xFFFF8800u; // orange

        jar.item.state = new StateWriter(StateSize)
            .WriteUInt64(owner)
            .WriteUInt64(deployedAt)
            .WriteUInt32(paintArgb)
            .WriteString("Look up!", MessageMax)
            .ToArray();
    }
}

// Read it back later:
public (ulong owner, ulong deployedAt, uint paint, string msg) DecodeFlare(ItemJar jar)
{
    var r = new StateReader(jar.item.state);
    return (r.ReadUInt64(), r.ReadUInt64(), r.ReadUInt32(), r.ReadString(64));
}
```
