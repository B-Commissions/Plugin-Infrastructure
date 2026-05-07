using BlueBeard.Core.Abstractions;
using Rocket.Unturned.Chat;
using Steamworks;
using UnityEngine;
using RocketLog = Rocket.Core.Logging.Logger;

namespace BlueBeard.RocketMod;

internal sealed class RocketChat : IChat
{
    public void Say(IPlayer player, string message, Color color = default)
    {
        var c = color == default ? Color.white : color;
        if (player == null || player.IsConsole)
        {
            RocketLog.Log(message);
            return;
        }
        UnturnedChat.Say(player.SteamId, message, c);
    }

    public void Say(CSteamID steamId, string message, Color color = default)
    {
        var c = color == default ? Color.white : color;
        UnturnedChat.Say(steamId, message, c);
    }

    public void Broadcast(string message, Color color = default)
    {
        var c = color == default ? Color.white : color;
        UnturnedChat.Say(message, c);
    }
}
