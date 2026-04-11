using System.Collections.Generic;
using SDG.Unturned;

namespace BlueBeard.MiniGames;

/// <summary>
/// A single in-progress mini-game session for a specific player. Created by
/// <see cref="MiniGameManager.Start"/> and mutated internally by the tick runner.
/// Handlers read <see cref="State"/>, <see cref="TimeRemaining"/>, and
/// <see cref="SessionData"/> during their lifecycle callbacks.
/// </summary>
public class MiniGameInstance
{
    public Player Player { get; }
    public MiniGameDefinition Definition { get; }

    /// <summary>Countdown timer in seconds. Decremented by the tick runner each frame.</summary>
    public float TimeRemaining { get; internal set; }

    /// <summary>Current lifecycle state. Transitions trigger handler callbacks and/or the completed event.</summary>
    public MiniGameState State { get; internal set; }

    /// <summary>
    /// Per-session scratch space for the handler. Handlers use this to track internal state
    /// (wires cut, keys entered, attempts remaining, etc.) without needing their own storage.
    /// </summary>
    public Dictionary<string, object> SessionData { get; } = new();

    internal MiniGameInstance(Player player, MiniGameDefinition definition)
    {
        Player = player;
        Definition = definition;
        TimeRemaining = definition.Duration;
        State = MiniGameState.Running;
    }
}
