using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace BlueBeard.Items.Behaviours;

/// <summary>
/// Registry that routes barricade events (spawned, damage, salvage, destroyed) to per-asset-id
/// handlers. Mirrors <see cref="ItemBehaviourManager"/>'s shape — register one handler per asset
/// id; spawn/damage/salvage are auto-dispatched, destroyed is manual via <see cref="NotifyDestroyed"/>.
/// </summary>
public class BarricadeBehaviourManager : EntityBehaviourRegistry<ushort, IBarricadeBehaviour>
{
    public override void Load()
    {
        BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
        BarricadeManager.onDamageBarricadeRequested += OnDamageRequested;
        BarricadeDrop.OnSalvageRequested_Global += OnSalvageRequested;
    }

    public override void Unload()
    {
        BarricadeManager.onBarricadeSpawned -= OnBarricadeSpawned;
        BarricadeManager.onDamageBarricadeRequested -= OnDamageRequested;
        BarricadeDrop.OnSalvageRequested_Global -= OnSalvageRequested;
        base.Unload();
    }

    /// <summary>Manually fire <see cref="IBarricadeBehaviour.OnDestroyed"/> for the given drop.</summary>
    public void NotifyDestroyed(BarricadeDrop drop)
    {
        if (drop?.asset == null) return;
        if (Behaviours.TryGetValue(drop.asset.id, out var b))
            b.OnDestroyed(drop);
    }

    private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
    {
        if (drop?.asset == null) return;
        if (Behaviours.TryGetValue(drop.asset.id, out var b))
            b.OnSpawned(drop);
    }

    private void OnDamageRequested(CSteamID instigator, Transform barricadeTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
        if (!shouldAllow) return;
        var drop = BarricadeManager.FindBarricadeByRootTransform(barricadeTransform);
        if (drop?.asset == null) return;
        if (Behaviours.TryGetValue(drop.asset.id, out var b))
        {
            if (!b.OnDamageRequested(instigator, drop, pendingTotalDamage, damageOrigin))
                shouldAllow = false;
        }
    }

    private void OnSalvageRequested(BarricadeDrop drop, SteamPlayer instigator, ref bool shouldAllow)
    {
        if (!shouldAllow) return;
        if (drop?.asset == null) return;
        if (Behaviours.TryGetValue(drop.asset.id, out var b))
        {
            if (!b.OnSalvageRequested(drop, instigator))
                shouldAllow = false;
        }
    }
}
