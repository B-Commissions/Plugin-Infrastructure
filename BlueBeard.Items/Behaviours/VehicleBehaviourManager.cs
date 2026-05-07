using SDG.Unturned;
using Steamworks;

namespace BlueBeard.Items.Behaviours;

/// <summary>
/// Registry that routes vehicle events (enter, damage, tire damage, siphon, lockpick) to
/// per-asset-id handlers. Exit and destroy hooks are manual.
/// </summary>
public class VehicleBehaviourManager : EntityBehaviourRegistry<ushort, IVehicleBehaviour>
{
    public override void Load()
    {
        VehicleManager.onEnterVehicleRequested += OnEnterRequested;
        VehicleManager.onDamageVehicleRequested += OnDamageRequested;
        VehicleManager.onDamageTireRequested += OnTireDamageRequested;
        VehicleManager.onSiphonVehicleRequested += OnSiphonRequested;
        VehicleManager.onVehicleLockpicked += OnLockpickRequested;
    }

    public override void Unload()
    {
        VehicleManager.onEnterVehicleRequested -= OnEnterRequested;
        VehicleManager.onDamageVehicleRequested -= OnDamageRequested;
        VehicleManager.onDamageTireRequested -= OnTireDamageRequested;
        VehicleManager.onSiphonVehicleRequested -= OnSiphonRequested;
        VehicleManager.onVehicleLockpicked -= OnLockpickRequested;
        base.Unload();
    }

    /// <summary>Manually fire <see cref="IVehicleBehaviour.OnDestroyed"/>.</summary>
    public void NotifyDestroyed(InteractableVehicle vehicle)
    {
        if (vehicle?.asset == null) return;
        if (Behaviours.TryGetValue(vehicle.asset.id, out var b))
            b.OnDestroyed(vehicle);
    }

    /// <summary>Manually fire <see cref="IVehicleBehaviour.OnExited"/>.</summary>
    public void NotifyExited(Player player, InteractableVehicle vehicle)
    {
        if (vehicle?.asset == null) return;
        if (Behaviours.TryGetValue(vehicle.asset.id, out var b))
            b.OnExited(player, vehicle);
    }

    private void OnEnterRequested(Player player, InteractableVehicle vehicle, ref bool shouldAllow)
    {
        if (!shouldAllow || vehicle?.asset == null) return;
        if (Behaviours.TryGetValue(vehicle.asset.id, out var b))
        {
            if (!b.OnEnterRequested(player, vehicle))
                shouldAllow = false;
        }
    }

    private void OnDamageRequested(CSteamID instigator, InteractableVehicle vehicle, ref ushort pendingTotalDamage, ref bool canRepair, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
        if (!shouldAllow || vehicle?.asset == null) return;
        if (Behaviours.TryGetValue(vehicle.asset.id, out var b))
        {
            if (!b.OnDamageRequested(instigator, vehicle, pendingTotalDamage, canRepair, damageOrigin))
                shouldAllow = false;
        }
    }

    private void OnTireDamageRequested(CSteamID instigator, InteractableVehicle vehicle, int tireIndex, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
        if (!shouldAllow || vehicle?.asset == null) return;
        if (Behaviours.TryGetValue(vehicle.asset.id, out var b))
        {
            if (!b.OnTireDamageRequested(instigator, vehicle, tireIndex, damageOrigin))
                shouldAllow = false;
        }
    }

    private void OnSiphonRequested(InteractableVehicle vehicle, Player instigator, ref bool shouldAllow, ref ushort desiredAmount)
    {
        if (!shouldAllow || vehicle?.asset == null) return;
        if (Behaviours.TryGetValue(vehicle.asset.id, out var b))
        {
            if (!b.OnSiphonRequested(vehicle, instigator, desiredAmount))
                shouldAllow = false;
        }
    }

    private void OnLockpickRequested(InteractableVehicle vehicle, Player instigator, ref bool allow)
    {
        if (!allow || vehicle?.asset == null) return;
        if (Behaviours.TryGetValue(vehicle.asset.id, out var b))
        {
            if (!b.OnLockpickRequested(vehicle, instigator))
                allow = false;
        }
    }
}
