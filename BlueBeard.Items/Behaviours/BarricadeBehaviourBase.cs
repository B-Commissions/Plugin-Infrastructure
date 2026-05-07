using SDG.Unturned;
using Steamworks;

namespace BlueBeard.Items.Behaviours;

/// <summary>
/// Convenience base for <see cref="IBarricadeBehaviour"/>. Every hook is a virtual no-op so
/// subclasses override only what they need. Damage and salvage default to allow.
/// </summary>
public abstract class BarricadeBehaviourBase : IBarricadeBehaviour
{
    public virtual void OnSpawned(BarricadeDrop drop) { }
    public virtual bool OnDamageRequested(CSteamID instigator, BarricadeDrop drop, ushort pendingDamage, EDamageOrigin damageOrigin) => true;
    public virtual bool OnSalvageRequested(BarricadeDrop drop, SteamPlayer instigator) => true;
    public virtual void OnDestroyed(BarricadeDrop drop) { }
}
