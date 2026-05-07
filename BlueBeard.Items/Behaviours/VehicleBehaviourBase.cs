using SDG.Unturned;
using Steamworks;

namespace BlueBeard.Items.Behaviours;

public abstract class VehicleBehaviourBase : IVehicleBehaviour
{
    public virtual bool OnEnterRequested(Player player, InteractableVehicle vehicle) => true;
    public virtual bool OnDamageRequested(CSteamID instigator, InteractableVehicle vehicle, ushort pendingDamage, bool canRepair, EDamageOrigin damageOrigin) => true;
    public virtual bool OnTireDamageRequested(CSteamID instigator, InteractableVehicle vehicle, int tireIndex, EDamageOrigin damageOrigin) => true;
    public virtual bool OnSiphonRequested(InteractableVehicle vehicle, Player instigator, ushort desiredAmount) => true;
    public virtual bool OnLockpickRequested(InteractableVehicle vehicle, Player instigator) => true;
    public virtual void OnDestroyed(InteractableVehicle vehicle) { }
    public virtual void OnExited(Player player, InteractableVehicle vehicle) { }
}
