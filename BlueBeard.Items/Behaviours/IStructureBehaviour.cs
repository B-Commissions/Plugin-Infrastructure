using SDG.Unturned;
using Steamworks;

namespace BlueBeard.Items.Behaviours;

/// <summary>
/// Server-side behaviour for a specific structure asset id, registered with
/// <c>StructureBehaviourManager.Register</c>. Inherit <see cref="StructureBehaviourBase"/>
/// for virtual no-op defaults.
/// </summary>
public interface IStructureBehaviour
{
    /// <summary>Structure of this asset id was just placed in the world.</summary>
    void OnSpawned(StructureDrop drop);

    /// <summary>
    /// Damage is about to be applied. Return false to prevent it.
    /// Auto-wired via <see cref="StructureManager.onDamageStructureRequested"/>.
    /// </summary>
    bool OnDamageRequested(CSteamID instigator, StructureDrop drop, ushort pendingDamage, EDamageOrigin damageOrigin);

    /// <summary>
    /// Structure was destroyed. Unturned has no global destroyed event, so this is invoked
    /// manually via <see cref="StructureBehaviourManager.NotifyDestroyed"/>.
    /// </summary>
    void OnDestroyed(StructureDrop drop);
}
