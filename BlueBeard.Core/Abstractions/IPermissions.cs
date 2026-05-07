using Steamworks;

namespace BlueBeard.Core.Abstractions;

/// <summary>
/// Framework-agnostic permissions facade. Calls are synchronous; the OpenMod adapter
/// wraps OpenMod's async API with <c>.GetAwaiter().GetResult()</c> on the main thread.
/// </summary>
public interface IPermissions
{
    bool HasPermission(IPlayer player, string permission);
    bool HasPermission(CSteamID steamId, string permission);

    void AddPlayerToGroup(string groupName, IPlayer player);
    void RemovePlayerFromGroup(string groupName, IPlayer player);
}
