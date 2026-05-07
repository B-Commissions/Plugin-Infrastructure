using SDG.Unturned;
using Steamworks;

namespace BlueBeard.Items.Behaviours;

/// <summary>
/// Server-side behaviour for a specific vehicle asset id, registered with
/// <c>VehicleBehaviourManager.Register</c>. Inherit <see cref="VehicleBehaviourBase"/>
/// for virtual no-op defaults.
/// </summary>
public interface IVehicleBehaviour
{
    /// <summary>Player is attempting to enter. Return false to prevent.</summary>
    bool OnEnterRequested(Player player, InteractableVehicle vehicle);

    /// <summary>Damage is about to be applied. Return false to prevent.</summary>
    bool OnDamageRequested(CSteamID instigator, InteractableVehicle vehicle, ushort pendingDamage, bool canRepair, EDamageOrigin damageOrigin);

    /// <summary>Tire damage is about to be applied. Return false to prevent.</summary>
    bool OnTireDamageRequested(CSteamID instigator, InteractableVehicle vehicle, int tireIndex, EDamageOrigin damageOrigin);

    /// <summary>Player is siphoning fuel. Return false to prevent.</summary>
    bool OnSiphonRequested(InteractableVehicle vehicle, Player instigator, ushort desiredAmount);

    /// <summary>Player is lockpicking. Return false to prevent.</summary>
    bool OnLockpickRequested(InteractableVehicle vehicle, Player instigator);

    /// <summary>Vehicle was destroyed. Invoked manually via <see cref="VehicleBehaviourManager.NotifyDestroyed"/>.</summary>
    void OnDestroyed(InteractableVehicle vehicle);

    /// <summary>Player exited. Invoked manually via <see cref="VehicleBehaviourManager.NotifyExited"/>.</summary>
    void OnExited(Player player, InteractableVehicle vehicle);
}
