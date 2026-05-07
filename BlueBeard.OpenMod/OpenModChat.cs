using System;
using BlueBeard.Core.Abstractions;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace BlueBeard.OpenMod;

/// <summary>
/// <see cref="IChat"/> adapter that talks to Unturned's chat system directly via
/// <see cref="ChatManager"/>. OpenMod ships its own chat services, but routing through
/// the SDG layer keeps message delivery identical to the Rocket adapter and avoids
/// requiring extra DI services.
/// </summary>
public sealed class OpenModChat : IChat
{
    private readonly IUnturnedUserDirectory _userDirectory;

    public OpenModChat(IUnturnedUserDirectory userDirectory)
    {
        _userDirectory = userDirectory ?? throw new ArgumentNullException(nameof(userDirectory));
    }

    public void Say(IPlayer player, string message, Color color = default)
    {
        if (player == null || player.IsConsole)
        {
            UnityEngine.Debug.Log(message);
            return;
        }
        Say(player.SteamId, message, color);
    }

    public void Say(CSteamID steamId, string message, Color color = default)
    {
        var user = _userDirectory.FindUser(steamId);
        if (user == null) return;
        var c = color == default ? Color.white : color;
        ChatManager.serverSendMessage(message, c, toPlayer: user.Player.SteamPlayer);
    }

    public void Broadcast(string message, Color color = default)
    {
        var c = color == default ? Color.white : color;
        ChatManager.serverSendMessage(message, c);
    }
}
