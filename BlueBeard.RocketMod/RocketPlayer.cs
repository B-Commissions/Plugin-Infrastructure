using BlueBeard.Core.Abstractions;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using RocketLog = Rocket.Core.Logging.Logger;

namespace BlueBeard.RocketMod;

/// <summary>
/// <see cref="IPlayer"/> wrapper around RocketMod's <see cref="UnturnedPlayer"/>. Use the
/// static factories <see cref="From(UnturnedPlayer)"/> / <see cref="From(Player)"/> /
/// <see cref="Console"/> rather than constructing directly.
/// </summary>
public sealed class RocketPlayer : IPlayer
{
    private readonly UnturnedPlayer _player;
    private readonly bool _isConsole;

    private RocketPlayer(UnturnedPlayer player, bool isConsole)
    {
        _player = player;
        _isConsole = isConsole;
    }

    public static RocketPlayer From(UnturnedPlayer player) => new(player, isConsole: false);
    public static RocketPlayer From(Player player) => new(UnturnedPlayer.FromPlayer(player), isConsole: false);
    public static RocketPlayer Console { get; } = new(null, isConsole: true);

    public CSteamID SteamId => _isConsole ? CSteamID.Nil : _player.CSteamID;
    public string DisplayName => _isConsole ? "Console" : _player?.DisplayName ?? "<unknown>";
    public Vector3 Position => _isConsole ? Vector3.zero : _player.Position;
    public bool IsConsole => _isConsole;
    public Player Unturned => _isConsole ? null : _player?.Player;

    public bool HasPermission(string permission)
    {
        if (_isConsole) return true;
        return _player.HasPermission(permission);
    }

    public void SendMessage(string message, Color color = default)
    {
        var c = color == default ? Color.white : color;
        if (_isConsole)
            RocketLog.Log(message);
        else
            UnturnedChat.Say(_player, message, c);
    }
}
