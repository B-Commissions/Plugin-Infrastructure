using BlueBeard.Zones.Tracking;
using Rocket.Unturned.Chat;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace BlueBeard.Zones.Flags.Handlers;

public class AccessFlagHandler : FlagHandlerBase
{
    public AccessFlagHandler(ZoneManager zoneManager, PlayerTracker playerTracker)
        : base(zoneManager, playerTracker) { }

    public override string FlagName => "access";

    public override void Subscribe()
    {
        ZoneManager.PlayerEnteredZone += OnPlayerEntered;
        ZoneManager.PlayerExitedZone += OnPlayerExited;
        VehicleManager.onEnterVehicleRequested += OnEnterVehicle;
    }

    public override void Unsubscribe()
    {
        ZoneManager.PlayerEnteredZone -= OnPlayerEntered;
        ZoneManager.PlayerExitedZone -= OnPlayerExited;
        VehicleManager.onEnterVehicleRequested -= OnEnterVehicle;
    }

    private void OnPlayerEntered(Player player, ZoneDefinition definition)
    {
        if (definition.Flags == null || !definition.Flags.ContainsKey(ZoneFlag.NoEnter)) return;
        if (HasOverridePermission(player, ZoneFlag.NoEnter, definition.Id)) return;

        // Teleport player back out
        var direction = (player.transform.position - definition.Center).normalized;
        var teleportPos = player.transform.position + direction * 3f;
        player.teleportToLocationUnsafe(teleportPos, player.transform.rotation.eulerAngles.y);
        UnturnedChat.Say(player.channel.owner.playerID.steamID, "You are not allowed to enter this zone.", Color.red);
    }

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
