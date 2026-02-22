using BlueBeard.Zones.Tracking;
using SDG.Unturned;

namespace BlueBeard.Zones.Flags.Handlers;

public class LockpickFlagHandler : FlagHandlerBase
{
    public LockpickFlagHandler(ZoneManager zoneManager, PlayerTracker playerTracker)
        : base(zoneManager, playerTracker) { }

    public override string FlagName => ZoneFlag.NoLockpick;

    public override void Subscribe()
    {
        VehicleManager.onVehicleLockpicked += OnVehicleLockpicked;
    }

    public override void Unsubscribe()
    {
        VehicleManager.onVehicleLockpicked -= OnVehicleLockpicked;
    }

    private void OnVehicleLockpicked(InteractableVehicle vehicle, Player instigatingPlayer, ref bool allow)
    {
        if (!allow) return;
        if (instigatingPlayer == null) return;

        if (IsPlayerInZoneWithFlag(instigatingPlayer, ZoneFlag.NoLockpick, out var zone, out _))
        {
            if (HasOverridePermission(instigatingPlayer, ZoneFlag.NoLockpick, zone.Id)) return;
            allow = false;
        }
    }
}
