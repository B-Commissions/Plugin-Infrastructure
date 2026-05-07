using BlueBeard.Core.Abstractions;
using Rocket.API;
using Rocket.Core;
using Rocket.Unturned.Player;
using Steamworks;

namespace BlueBeard.RocketMod;

internal sealed class RocketPermissions : IPermissions
{
    public bool HasPermission(IPlayer player, string permission)
    {
        if (player == null) return false;
        if (player.IsConsole) return true;
        return player.HasPermission(permission);
    }

    public bool HasPermission(CSteamID steamId, string permission)
    {
        var player = UnturnedPlayer.FromCSteamID(steamId);
        return player != null && player.HasPermission(permission);
    }

    public void AddPlayerToGroup(string groupName, IPlayer player)
    {
        if (player == null || player.IsConsole) return;
        var rocketPlayer = UnturnedPlayer.FromCSteamID(player.SteamId);
        if (rocketPlayer != null)
            R.Permissions.AddPlayerToGroup(groupName, rocketPlayer);
    }

    public void RemovePlayerFromGroup(string groupName, IPlayer player)
    {
        if (player == null || player.IsConsole) return;
        var rocketPlayer = UnturnedPlayer.FromCSteamID(player.SteamId);
        if (rocketPlayer != null)
            R.Permissions.RemovePlayerFromGroup(groupName, rocketPlayer);
    }
}
