using SDG.Unturned;
using Steamworks;

namespace BlueBeard.Items.Behaviours;

public abstract class StructureBehaviourBase : IStructureBehaviour
{
    public virtual void OnSpawned(StructureDrop drop) { }
    public virtual bool OnDamageRequested(CSteamID instigator, StructureDrop drop, ushort pendingDamage, EDamageOrigin damageOrigin) => true;
    public virtual void OnDestroyed(StructureDrop drop) { }
}
