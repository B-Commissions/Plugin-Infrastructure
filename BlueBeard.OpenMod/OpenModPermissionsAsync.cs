using System;
using System.Threading.Tasks;
using BlueBeard.Core.Abstractions;
using OpenMod.API.Permissions;
using OpenMod.Core.Permissions;
using OpenMod.Unturned.Users;
using Steamworks;

namespace BlueBeard.OpenMod;

/// <summary>
/// Natively async <see cref="IPermissionsAsync"/> for OpenMod — no sync-over-async
/// blocking, unlike the synchronous <see cref="OpenModPermissions"/> compatibility shim.
/// Installed automatically by <see cref="OpenModBootstrap.Install"/>.
/// Online players only (actors resolve through the connected-user directory).
/// </summary>
public sealed class OpenModPermissionsAsync : IPermissionsAsync
{
    private readonly IPermissionChecker _checker;
    private readonly IPermissionRoleStore _roleStore;
    private readonly IUnturnedUserDirectory _userDirectory;

    public OpenModPermissionsAsync(
        IPermissionChecker checker,
        IPermissionRoleStore roleStore,
        IUnturnedUserDirectory userDirectory)
    {
        _checker = checker ?? throw new ArgumentNullException(nameof(checker));
        _roleStore = roleStore ?? throw new ArgumentNullException(nameof(roleStore));
        _userDirectory = userDirectory ?? throw new ArgumentNullException(nameof(userDirectory));
    }

    public Task<bool> HasPermissionAsync(IPlayer player, string permission)
    {
        if (player == null) return Task.FromResult(false);
        if (player.IsConsole) return Task.FromResult(true);
        return HasPermissionAsync(player.SteamId, permission);
    }

    public async Task<bool> HasPermissionAsync(CSteamID steamId, string permission)
    {
        var actor = _userDirectory.FindUser(steamId);
        if (actor == null) return false;
        var result = await _checker.CheckPermissionAsync(actor, permission).ConfigureAwait(false);
        return result == PermissionGrantResult.Grant;
    }

    public async Task AddPlayerToGroupAsync(string groupName, IPlayer player)
    {
        if (player == null || player.IsConsole) return;
        var actor = _userDirectory.FindUser(player.SteamId);
        if (actor == null) return;
        await _roleStore.AddRoleToActorAsync(actor, groupName).ConfigureAwait(false);
    }

    public async Task RemovePlayerFromGroupAsync(string groupName, IPlayer player)
    {
        if (player == null || player.IsConsole) return;
        var actor = _userDirectory.FindUser(player.SteamId);
        if (actor == null) return;
        await _roleStore.RemoveRoleFromActorAsync(actor, groupName).ConfigureAwait(false);
    }
}
