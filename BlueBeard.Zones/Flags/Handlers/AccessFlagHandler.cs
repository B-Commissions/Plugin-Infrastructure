using System.Linq;
using BlueBeard.Zones.Tracking;
using Rocket.Unturned.Chat;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace BlueBeard.Zones.Flags.Handlers;

public class AccessFlagHandler(ZoneManager zoneManager, PlayerTracker playerTracker) : FlagHandlerBase(zoneManager, playerTracker)
{
    public override string FlagName => "access";

    public override void Subscribe()
    {
        // Tracker events are height/shape-filtered; raw ZoneManager events are not.
        PlayerTracker.PlayerEnteredZone += OnPlayerEntered;
        PlayerTracker.PlayerExitedZone += OnPlayerExited;
        VehicleManager.onEnterVehicleRequested += OnEnterVehicle;
    }

    public override void Unsubscribe()
    {
        PlayerTracker.PlayerEnteredZone -= OnPlayerEntered;
        PlayerTracker.PlayerExitedZone -= OnPlayerExited;
        VehicleManager.onEnterVehicleRequested -= OnEnterVehicle;
    }

    private void OnPlayerEntered(Player player, ZoneDefinition definition)
    {
        if (definition.Flags == null || !definition.Flags.ContainsKey(ZoneFlag.NoEnter)) return;
        if (HasOverridePermission(player, ZoneFlag.NoEnter, definition.Id)) return;

        // Push the player clear of the zone. A fixed 3m nudge from the current position
        // failed for large zones (still inside -> retrigger loop) and produced a zero
        // vector for a player at the exact center (stuck + message spam).
        var position = player.transform.position;
        var horizontal = position - definition.Center;
        horizontal.y = 0;
        var direction = horizontal.sqrMagnitude > 0.0001f ? horizontal.normalized : Vector3.forward;

        var teleportPos = definition.Center + direction * (GetZoneExtent(definition) + 2f);
        teleportPos.y = position.y;
        player.teleportToLocationUnsafe(teleportPos, player.transform.rotation.eulerAngles.y);
        UnturnedChat.Say(player.channel.owner.playerID.steamID, "You are not allowed to enter this zone.", Color.red);
    }

    /// <summary>Horizontal distance from center that guarantees being outside the shape.</summary>
    private static float GetZoneExtent(ZoneDefinition definition) => definition.Shape switch
    {
        Shapes.RadiusZoneShape radius => radius.Radius,
        Shapes.PolygonZoneShape polygon => polygon.WorldPoints
            .Max(v => new Vector2(v.x - definition.Center.x, v.z - definition.Center.z).magnitude),
        _ => 3f
    };

    private void OnPlayerExited(Player player, ZoneDefinition definition)
    {
        if (definition.Flags == null || !definition.Flags.ContainsKey(ZoneFlag.NoLeave)) return;
        if (HasOverridePermission(player, ZoneFlag.NoLeave, definition.Id)) return;

        // Teleport player back in
        player.teleportToLocationUnsafe(definition.Center, player.transform.rotation.eulerAngles.y);
        UnturnedChat.Say(player.channel.owner.playerID.steamID, "You are not allowed to leave this zone.", Color.red);
    }

    private void OnEnterVehicle(Player player, InteractableVehicle vehicle, ref bool shouldAllow)
    {
        if (!shouldAllow) return;

        if (IsPlayerInZoneWithFlag(player, ZoneFlag.NoVehicleCarjack, out var zone, out _))
        {
            // Only block if the player is not the owner
            if (vehicle.lockedOwner != CSteamID.Nil &&
                vehicle.lockedOwner != player.channel.owner.playerID.steamID)
            {
                if (!HasOverridePermission(player, ZoneFlag.NoVehicleCarjack, zone.Id))
                {
                    shouldAllow = false;
                }
            }
        }
    }
}
