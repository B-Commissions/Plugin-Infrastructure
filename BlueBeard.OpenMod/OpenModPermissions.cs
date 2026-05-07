using System;
using BlueBeard.Core.Abstractions;
using OpenMod.API.Permissions;
using OpenMod.API.Users;
using OpenMod.Core.Permissions;
using OpenMod.Unturned.Users;
using Steamworks;

namespace BlueBeard.OpenMod;

/// <summary>
/// <see cref="IPermissions"/> adapter for OpenMod. Wraps OpenMod's async
/// <see cref="IPermissionChecker"/> and <see cref="IPermissionRoleStore"/> with synchronous
/// calls — safe on the Unturned main thread because permission checks are typically backed
/// by in-memory or fast-storage providers.
/// </summary>
public sealed class OpenModPermissions : IPermissions
{
    private readonly IPermissionChecker _checker;
    private readonly IPermissionRoleStore _roleStore;
    private readonly IUnturnedUserDirectory _userDirectory;

    public OpenModPermissions(
        IPermissionChecker checker,
        IPermissionRoleStore roleStore,
        IUnturnedUserDirectory userDirectory)
    {
        _checker = checker ?? throw new ArgumentNullException(nameof(checker));
        _roleStore = roleStore ?? throw new ArgumentNullException(nameof(roleStore));
        _userDirectory = userDirectory ?? throw new ArgumentNullException(nameof(userDirectory));
    }

    public bool HasPermission(IPlayer player, string permission)
    {
        if (player == null) return false;
        if (player.IsConsole) return true;
        return HasPermission(player.SteamId, permission);
    }

    public bool HasPermission(CSteamID steamId, string permission)
    {
        var actor = ResolveActor(steamId);
        if (actor == null) return false;
        var result = _checker.CheckPermissionAsync(actor, permission).ConfigureAwait(false).GetAwaiter().GetResult();
        return result == PermissionGrantResult.Grant;
    }

    public void AddPlayerToGroup(string groupName, IPlayer player)
    {
        if (player == null || player.IsConsole) return;
        var actor = ResolveActor(player.SteamId);
        if (actor == null) return;
        _roleStore.AddRoleToActorAsync(actor, groupName).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public void RemovePlayerFromGroup(string groupName, IPlayer player)
    {
        if (player == null || player.IsConsole) return;
        var actor = ResolveActor(player.SteamId);
        if (actor == null) return;
        _roleStore.RemoveRoleFromActorAsync(actor, groupName).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private IPermissionActor ResolveActor(CSteamID steamId)
    {
        var user = _userDirectory.FindUser(steamId);
        return user;
    }
}
