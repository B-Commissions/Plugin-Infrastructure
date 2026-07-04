using System.Threading.Tasks;
using Steamworks;

namespace BlueBeard.Core.Abstractions;

/// <summary>
/// Async permissions facade. Prefer this over <see cref="IPermissions"/> in async code
/// paths: the OpenMod adapter implements it natively (no sync-over-async blocking), and
/// under RocketMod it wraps the synchronous API at zero cost.
/// Access via <see cref="BlueBeardHost.PermissionsAsync"/>.
/// </summary>
public interface IPermissionsAsync
{
    Task<bool> HasPermissionAsync(IPlayer player, string permission);
    Task<bool> HasPermissionAsync(CSteamID steamId, string permission);

    Task AddPlayerToGroupAsync(string groupName, IPlayer player);
    Task RemovePlayerFromGroupAsync(string groupName, IPlayer player);
}

/// <summary>
/// Default <see cref="IPermissionsAsync"/> that wraps a synchronous <see cref="IPermissions"/>.
/// Used automatically when the adapter doesn't install a native async implementation.
/// </summary>
public sealed class SyncPermissionsAsyncWrapper(IPermissions inner) : IPermissionsAsync
{
    public Task<bool> HasPermissionAsync(IPlayer player, string permission) =>
        Task.FromResult(inner.HasPermission(player, permission));

    public Task<bool> HasPermissionAsync(CSteamID steamId, string permission) =>
        Task.FromResult(inner.HasPermission(steamId, permission));

    public Task AddPlayerToGroupAsync(string groupName, IPlayer player)
    {
        inner.AddPlayerToGroup(groupName, player);
        return Task.CompletedTask;
    }

    public Task RemovePlayerFromGroupAsync(string groupName, IPlayer player)
    {
        inner.RemovePlayerFromGroup(groupName, player);
        return Task.CompletedTask;
    }
}
