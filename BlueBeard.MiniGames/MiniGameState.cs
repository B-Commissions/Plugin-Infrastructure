namespace BlueBeard.MiniGames;

/// <summary>Lifecycle states for a <see cref="MiniGameInstance"/>.</summary>
public enum MiniGameState
{
    /// <summary>The mini-game is currently running and accepting input.</summary>
    Running,

    /// <summary>The player completed the mini-game successfully.</summary>
    Succeeded,

    /// <summary>The player failed the mini-game (wrong input, incorrect answer, etc.).</summary>
    Failed,

    /// <summary>The duration elapsed before the player completed the mini-game.</summary>
    TimedOut,

    /// <summary>The mini-game was cancelled externally (by a command, another mini-game starting, etc.).</summary>
    Cancelled,
}
