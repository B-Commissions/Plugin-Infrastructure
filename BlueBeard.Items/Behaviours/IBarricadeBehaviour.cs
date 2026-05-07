using SDG.Unturned;
using Steamworks;

namespace BlueBeard.Items.Behaviours;

/// <summary>
/// Server-side behaviour for a specific barricade asset id, registered with
/// <c>BarricadeBehaviourManager.Register</c>. Inherit <see cref="BarricadeBehaviourBase"/>
/// for virtual no-op defaults.
/// </summary>
public interface IBarricadeBehaviour
{
    /// <summary>Barricade of this asset id was just placed in the world.</summary>
    void OnSpawned(BarricadeDrop drop);

    /// <summary>
    /// Damage is about to be applied. Return false to prevent it.
    /// Auto-wired via <see cref="BarricadeManager.onDamageBarricadeRequested"/>.
    /// </summary>
    bool OnDamageRequested(CSteamID instigator, BarricadeDrop drop, ushort pendingDamage, EDamageOrigin damageOrigin);

    /// <summary>
    /// Player is attempting to salvage. Return false to prevent it.
    /// Auto-wired via <see cref="BarricadeDrop.OnSalvageRequested_Global"/>.
    /// </summary>
    bool OnSalvageRequested(BarricadeDrop drop, SteamPlayer instigator);

    /// <summary>
    /// Barricade was destroyed. Unturned has no global destroyed event, so this is invoked
    /// manually via <see cref="BarricadeBehaviourManager.NotifyDestroyed"/>.
    /// </summary>
    void OnDestroyed(BarricadeDrop drop);
}
