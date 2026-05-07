using Steamworks;
using UnityEngine;

namespace BlueBeard.Core.Abstractions;

/// <summary>
/// Framework-agnostic chat / message-broadcast facade. Adapters route to RocketMod's
/// <c>UnturnedChat</c> or OpenMod's <c>IChatService</c>.
/// </summary>
public interface IChat
{
    /// <summary>Send a message to a single player (or the console).</summary>
    void Say(IPlayer player, string message, Color color = default);

    /// <summary>Send a message to a single Steam ID.</summary>
    void Say(CSteamID steamId, string message, Color color = default);

    /// <summary>Send a message to every connected player.</summary>
    void Broadcast(string message, Color color = default);
}
