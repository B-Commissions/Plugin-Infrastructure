using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace BlueBeard.Core.Abstractions;

/// <summary>
/// Framework-agnostic player abstraction. Wraps the underlying SDG <see cref="Player"/> and
/// exposes the operations BlueBeard libraries actually need (identity, location, messaging,
/// permissions). Adapters for RocketMod and OpenMod implement this around their respective
/// player types.
/// </summary>
public interface IPlayer
{
    /// <summary>Steam ID, or <see cref="CSteamID.Nil"/> for the console / system actor.</summary>
    CSteamID SteamId { get; }

    /// <summary>Display name. For the console this is typically "Console" or similar.</summary>
    string DisplayName { get; }

    /// <summary>World position. Returns <see cref="Vector3.zero"/> for non-located actors (console).</summary>
    Vector3 Position { get; }

    /// <summary>True when this represents the server console rather than a connected player.</summary>
    bool IsConsole { get; }

    /// <summary>The underlying SDG player, or null for console.</summary>
    Player Unturned { get; }

    /// <summary>Check a permission node against the active permissions provider.</summary>
    bool HasPermission(string permission);

    /// <summary>Send a chat message to this actor (writes to log for console).</summary>
    void SendMessage(string message, Color color = default);
}
