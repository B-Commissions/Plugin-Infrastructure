using System;
using BlueBeard.Core.Abstractions;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace BlueBeard.OpenMod;

/// <summary>
/// <see cref="IPlayer"/> wrapper around OpenMod's <see cref="UnturnedUser"/>.
/// Use <see cref="From"/> or <see cref="Console"/> rather than constructing directly.
/// </summary>
public sealed class OpenModPlayer : IPlayer
{
    private readonly UnturnedUser _user;
    private readonly bool _isConsole;

    private OpenModPlayer(UnturnedUser user, bool isConsole)
    {
        _user = user;
        _isConsole = isConsole;
    }

    public static OpenModPlayer From(UnturnedUser user)
        => new(user ?? throw new ArgumentNullException(nameof(user)), isConsole: false);

    public static OpenModPlayer Console { get; } = new(null, isConsole: true);

    public CSteamID SteamId => _isConsole ? CSteamID.Nil : _user.SteamId;
    public string DisplayName => _isConsole ? "Console" : (_user?.DisplayName ?? "<unknown>");
    public Vector3 Position => _isConsole ? Vector3.zero : (_user?.Player?.Player?.transform?.position ?? Vector3.zero);
    public bool IsConsole => _isConsole;
    public Player Unturned => _isConsole ? null : _user?.Player?.Player;

    /// <summary>
    /// OpenMod's permission check is async; this synchronous accessor blocks on the
    /// result. Cached <see cref="OpenModPermissions"/> instance is required for the call —
    /// resolved via <see cref="BlueBeardHost.Permissions"/> at call time.
    /// </summary>
    public bool HasPermission(string permission)
    {
        if (_isConsole) return true;
        return BlueBeardHost.Permissions.HasPermission(this, permission);
    }

    public void SendMessage(string message, Color color = default)
    {
        BlueBeardHost.Chat.Say(this, message, color);
    }

    /// <summary>The underlying OpenMod user, for adapter consumers that need it.</summary>
    public UnturnedUser User => _user;
}
