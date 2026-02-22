using BlueBeard.Zones.Tracking;
using Rocket.API;
using Rocket.Core;
using SDG.Unturned;
using UnityEngine;

namespace BlueBeard.Zones.Flags;

public abstract class FlagHandlerBase : IFlagHandler
{
    protected readonly PlayerTracker PlayerTracker;
    protected readonly ZoneManager ZoneManager;

    protected FlagHandlerBase(ZoneManager zoneManager, PlayerTracker playerTracker)
    {
        ZoneManager = zoneManager;
        PlayerTracker = playerTracker;
    }

    public abstract string FlagName { get; }
    public abstract void Subscribe();
    public abstract void Unsubscribe();

    protected bool IsPlayerInZoneWithFlag(Player player, string flagName, out ZoneDefinition zone, out string flagValue)
    {
        return PlayerTracker.IsPlayerInZoneWithFlag(player, flagName, out zone, out flagValue);
    }

    protected bool IsPositionInZoneWithFlag(Vector3 position, string flagName, out ZoneDefinition zone, out string flagValue)
    {
        return PlayerTracker.IsPositionInZoneWithFlag(position, flagName, out zone, out flagValue);
    }

    protected bool HasOverridePermission(Player player, string flagName, string zoneId = null)
    {
        var rocketPlayer = Rocket.Unturned.Player.UnturnedPlayer.FromPlayer(player);
        if (rocketPlayer == null) return false;

        // Global override
        if (rocketPlayer.HasPermission($"zones.override.{flagName}"))
            return true;

        // Per-zone override
        if (zoneId != null && rocketPlayer.HasPermission($"zones.override.{flagName}.{zoneId}"))
            return true;

        return false;
    }
}
