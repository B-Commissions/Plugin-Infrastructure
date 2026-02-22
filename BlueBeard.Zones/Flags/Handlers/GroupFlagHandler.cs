using BlueBeard.Zones.Tracking;
using Rocket.Core;
using SDG.Unturned;

namespace BlueBeard.Zones.Flags.Handlers;

public class GroupFlagHandler : FlagHandlerBase
{
    public GroupFlagHandler(ZoneManager zoneManager, PlayerTracker playerTracker)
        : base(zoneManager, playerTracker) { }

    public override string FlagName => "group";

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
        var rocketPlayer = Rocket.Unturned.Player.UnturnedPlayer.FromPlayer(player);
        if (rocketPlayer == null) return;

        if (definition.Flags.TryGetValue(ZoneFlag.EnterAddGroup, out var addGroup) && !string.IsNullOrEmpty(addGroup))
        {
            R.Permissions.AddPlayerToGroup(addGroup, rocketPlayer);
        }

        if (definition.Flags.TryGetValue(ZoneFlag.EnterRemoveGroup, out var removeGroup) && !string.IsNullOrEmpty(removeGroup))
        {
            R.Permissions.RemovePlayerFromGroup(removeGroup, rocketPlayer);
        }
    }

    private void OnPlayerExited(Player player, ZoneDefinition definition)
    {
        if (definition.Flags == null) return;
        var rocketPlayer = Rocket.Unturned.Player.UnturnedPlayer.FromPlayer(player);
        if (rocketPlayer == null) return;

        if (definition.Flags.TryGetValue(ZoneFlag.LeaveAddGroup, out var addGroup) && !string.IsNullOrEmpty(addGroup))
        {
            R.Permissions.AddPlayerToGroup(addGroup, rocketPlayer);
        }

        if (definition.Flags.TryGetValue(ZoneFlag.LeaveRemoveGroup, out var removeGroup) && !string.IsNullOrEmpty(removeGroup))
        {
            R.Permissions.RemovePlayerFromGroup(removeGroup, rocketPlayer);
        }
    }
}
