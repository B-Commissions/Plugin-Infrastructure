namespace BlueBeard.MiniGames;

/// <summary>
/// Implementation contract for a specific mini-game type. The framework calls these hooks
/// at the appropriate points in the lifecycle; the handler is responsible for game logic
/// and updating the UI via <see cref="SDG.Unturned.EffectManager"/> calls.
/// </summary>
public interface IMiniGameHandler
{
    /// <summary>Called immediately after the mini-game effect is sent. Set up initial UI state.</summary>
    void OnStart(MiniGameInstance instance);

    /// <summary>Called every Unity frame while the mini-game is running. Update timer and animations.</summary>
    void OnTick(MiniGameInstance instance, float deltaTime);

    /// <summary>
    /// Called when the player interacts with the mini-game UI (button click or text input).
    /// <paramref name="inputName"/> is the effect button / input name; <paramref name="value"/>
    /// is empty for button clicks and holds the committed text for text inputs.
    /// </summary>
    void OnInput(MiniGameInstance instance, string inputName, string value);

    /// <summary>
    /// Called when the mini-game ends for any reason (success, failure, timeout, cancel).
    /// Clean up UI state and release any handler-specific resources.
    /// </summary>
    void OnEnd(MiniGameInstance instance);
}
