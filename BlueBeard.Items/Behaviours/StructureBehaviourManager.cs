using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace BlueBeard.Items.Behaviours;

/// <summary>
/// Registry that routes structure events to per-asset-id handlers. Spawn detection rides on
/// damage events because Unturned doesn't expose a global "structure spawned" signal; for
/// post-spawn hooks call <see cref="NotifySpawned"/> from your deploy hook.
/// </summary>
public class StructureBehaviourManager : EntityBehaviourRegistry<ushort, IStructureBehaviour>
{
    public override void Load()
    {
        StructureManager.onDamageStructureRequested += OnDamageRequested;
    }

    public override void Unload()
    {
        StructureManager.onDamageStructureRequested -= OnDamageRequested;
        base.Unload();
    }

    /// <summary>Manually fire <see cref="IStructureBehaviour.OnSpawned"/>.</summary>
    public void NotifySpawned(StructureDrop drop)
    {
        if (drop?.asset == null) return;
        if (Behaviours.TryGetValue(drop.asset.id, out var b))
            b.OnSpawned(drop);
    }

    /// <summary>Manually fire <see cref="IStructureBehaviour.OnDestroyed"/>.</summary>
    public void NotifyDestroyed(StructureDrop drop)
    {
        if (drop?.asset == null) return;
        if (Behaviours.TryGetValue(drop.asset.id, out var b))
            b.OnDestroyed(drop);
    }

    private void OnDamageRequested(CSteamID instigator, Transform structureTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
        if (!shouldAllow) return;
        var drop = StructureManager.FindStructureByRootTransform(structureTransform);
        if (drop?.asset == null) return;
        if (Behaviours.TryGetValue(drop.asset.id, out var b))
        {
            if (!b.OnDamageRequested(instigator, drop, pendingTotalDamage, damageOrigin))
                shouldAllow = false;
        }
    }
}
