using BlueBeard.Zones.Tracking;
using Rocket.Unturned.Chat;
using SDG.Unturned;
using UnityEngine;

namespace BlueBeard.Zones.Flags.Handlers;

public class NotificationFlagHandler : FlagHandlerBase
{
    public NotificationFlagHandler(ZoneManager zoneManager, PlayerTracker playerTracker)
        : base(zoneManager, playerTracker) { }

    public override string FlagName => "notification";

    public override void Subscribe()
    {
        ZoneManager.PlayerEnteredZone += OnPlayerEntered;
        ZoneManager.PlayerExitedZone += OnPlayerExited;
    }

    public override void Unsubscribe()
    {
        ZoneManager.PlayerEnteredZone -= OnPlayerEntered;
        ZoneManager.PlayerExitedZone -= OnPlayerExited;
    }

    private void OnPlayerEntered(Player player, ZoneDefinition definition)
    {
        if (definition.Flags == null) return;
        if (definition.Flags.TryGetValue(ZoneFlag.EnterMessage, out var message) && !string.IsNullOrEmpty(message))
        {
            UnturnedChat.Say(player.channel.owner.playerID.steamID, message, Color.green);
        }
    }

    private void OnPlayerExited(Player player, ZoneDefinition definition)
    {
        if (definition.Flags == null) return;
        if (definition.Flags.TryGetValue(ZoneFlag.LeaveMessage, out var message) && !string.IsNullOrEmpty(message))
        {
            UnturnedChat.Say(player.channel.owner.playerID.steamID, message, Color.yellow);
        }
    }
}
