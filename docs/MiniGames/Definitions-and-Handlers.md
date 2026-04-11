# Definitions and Handlers

## MiniGameDefinition

Static configuration for a mini-game type.

| Property | Type | Purpose |
|----------|------|---------|
| `Id` | `string` | Unique identifier. Must match the `miniGameId` passed to `RegisterHandler`. |
| `EffectId` | `ushort` | Unturned effect asset id used to render the mini-game UI. |
| `Duration` | `float` | Total time allowed, in seconds. The tick runner auto-completes with `TimedOut` when this elapses. |
| `AllowRetry` | `bool` | Whether the player can immediately restart after a failure (the framework does not enforce this -- it's a hint for the handler's own state management). |
| `Parameters` | `Dictionary<string, object>` | Arbitrary config passed to the handler (difficulty, wire count, colour set, etc.). |

Definitions are reusable and immutable -- one definition can be started many times for different players. Per-session state belongs in `MiniGameInstance.SessionData`, not here.

Example:

```csharp
var easy = new MiniGameDefinition
{
    Id = "hotwire",
    EffectId = 50710,
    Duration = 8f,
    AllowRetry = true,
};
easy.Parameters["wireCount"] = 3;

var hard = new MiniGameDefinition
{
    Id = "hotwire",
    EffectId = 50710,
    Duration = 4f,
    AllowRetry = false,
};
hard.Parameters["wireCount"] = 5;
```

Both use the same handler (`Id = "hotwire"`) but configure different difficulty.

## MiniGameInstance

A single in-progress session for a specific player.

| Property | Type | Notes |
|----------|------|-------|
| `Player` | `SDG.Unturned.Player` | The target player. |
| `Definition` | `MiniGameDefinition` | The definition this session was started from. |
| `TimeRemaining` | `float` | Countdown in seconds. Decremented by the tick runner each frame; handlers may read it but should not write to it (use `Complete` to force a result instead). |
| `State` | `MiniGameState` | `Running`, `Succeeded`, `Failed`, `TimedOut`, or `Cancelled`. |
| `SessionData` | `Dictionary<string, object>` | Per-session scratch space for the handler. |

Instances are created by `MiniGameManager.Start` and destroyed (the tick runner is destroyed) inside `Complete` or `Cancel`.

## IMiniGameHandler

Implementation contract for a specific mini-game type. One handler is registered per `Id` via `RegisterHandler`.

```csharp
public interface IMiniGameHandler
{
    void OnStart(MiniGameInstance instance);
    void OnTick(MiniGameInstance instance, float deltaTime);
    void OnInput(MiniGameInstance instance, string inputName, string value);
    void OnEnd(MiniGameInstance instance);
}
```

### OnStart
Called immediately after the mini-game effect is sent and the instance is added to the active set. Use this to:

- Populate `SessionData` with randomised or player-specific state.
- Send initial UI text / images via `EffectManager.sendUIEffectText` / `sendUIEffectImageURL`.
- Grant or debit any resources the mini-game needs up front.

### OnTick
Called every Unity `Update` frame while `State == Running`. `deltaTime` is the Unity `Time.deltaTime` for that frame. Use this to:

- Refresh on-screen timer / progress display.
- Drive any animation or procedural visual.
- Detect internal state changes that should auto-complete the game.

Avoid long-running work here -- `OnTick` runs on the main thread every frame.

### OnInput
Called when the player clicks a button or commits text in the mini-game UI. `inputName` is the effect button / input name; `value` is `string.Empty` for button clicks and the committed text for text inputs.

Typical shape:

```csharp
public void OnInput(MiniGameInstance i, string inputName, string value)
{
    switch (inputName)
    {
        case "Answer_Correct": MyPlugin.MiniGames.Complete(i, MiniGameState.Succeeded); break;
        case "Answer_Wrong":   MyPlugin.MiniGames.Complete(i, MiniGameState.Failed); break;
        case "Submit":
            if (value == ExpectedAnswer(i)) MyPlugin.MiniGames.Complete(i, MiniGameState.Succeeded);
            else                            MyPlugin.MiniGames.Complete(i, MiniGameState.Failed);
            break;
    }
}
```

### OnEnd
Called when the mini-game ends for any reason. The `instance.State` has already been set to the terminal value when `OnEnd` runs. Use this to:

- Send a closing UI effect (confetti, explosion, alarm, etc.).
- Award or debit resources based on `instance.State`.
- Unhook any per-session listeners.

Do not call `Complete` from `OnEnd` -- the instance is already in a terminal state.
