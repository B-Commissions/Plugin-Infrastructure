using System.Collections.Generic;

namespace BlueBeard.MiniGames;

/// <summary>
/// Static configuration for a mini-game type. One definition can be started many times for
/// different players; each <see cref="MiniGameManager.Start"/> call produces a fresh
/// <see cref="MiniGameInstance"/> that references the same definition.
/// </summary>
public class MiniGameDefinition
{
    /// <summary>Unique identifier for this mini-game type (matched against the registered handler id).</summary>
    public string Id { get; set; }

    /// <summary>The Unturned effect asset id used to render the mini-game UI.</summary>
    public ushort EffectId { get; set; }

    /// <summary>Total time allowed for the player to complete the mini-game, in seconds.</summary>
    public float Duration { get; set; }

    /// <summary>Whether the player can immediately restart after a failure without re-initiating.</summary>
    public bool AllowRetry { get; set; }

    /// <summary>Arbitrary per-definition parameters passed to the handler (difficulty, wire count, colour set...).</summary>
    public Dictionary<string, object> Parameters { get; } = new();
}
