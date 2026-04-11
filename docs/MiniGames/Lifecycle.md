# Lifecycle

A `MiniGameInstance` moves through a small state machine driven by `MiniGameManager`. Understanding the transitions and callbacks is the key to writing handlers that clean up correctly.

## States

```
            ┌──────────┐
 Start() ──▶│ Running  │
            └────┬─────┘
                 │
      ┌──────────┼──────────┬────────────┐
      ▼          ▼          ▼            ▼
 Succeeded    Failed    TimedOut    Cancelled
```

| State | Entered by | Typical cause |
|-------|------------|---------------|
| `Running` | `Start` | Newly started session. |
| `Succeeded` | `Complete(instance, Succeeded)` | Handler observed a win condition. |
| `Failed` | `Complete(instance, Failed)` | Handler observed a loss condition. |
| `TimedOut` | Tick runner, when `TimeRemaining <= 0` | Duration elapsed before any `Complete` call. |
| `Cancelled` | `Cancel(player)`, `Unload()`, or a new `Start` for the same player | External interruption. |

Transitions out of `Running` are one-way -- once a terminal state is set, the runner is destroyed and the instance is no longer active.

## Callbacks

| Transition | Callbacks, in order |
|------------|---------------------|
| `Start` | Effect sent → instance added to `_active` → `handler.OnStart(instance)` |
| `Running` frame | `instance.TimeRemaining -= dt` → `handler.OnTick(instance, dt)` → (if `TimeRemaining <= 0`) auto-complete with `TimedOut` |
| `EffectManager.onEffectButtonClicked` / `onEffectTextCommitted` | `handler.OnInput(instance, inputName, value)` |
| `Complete` / `Cancel` / auto-timeout | `instance.State = terminal` → effect cleared → tick runner destroyed → `handler.OnEnd(instance)` → `MiniGameCompleted?.Invoke(instance)` |

## One active per player

Only one mini-game can be active per player at a time. Calling `Start` while a previous mini-game is running for the same player:

1. Cancels the previous mini-game (transitions it to `Cancelled` and fires `OnEnd` + `MiniGameCompleted`).
2. Closes any open `BlueBeard.UI` for the player.
3. Starts the new mini-game.

If you want to *queue* mini-games instead of replacing, subscribe to `MiniGameCompleted` and start the next one from there.

## UI precedence rule

`MiniGameManager` and `UIManager` both hook `EffectManager.onEffectButtonClicked` / `onEffectTextCommitted`. If the player has a `BlueBeard.UI` open and a mini-game starts, `EffectManager` delivers every button click to BOTH systems. The framework resolves this by having `Start` close the open UI **before** sending the mini-game effect:

```csharp
public MiniGameInstance Start(Player player, MiniGameDefinition definition)
{
    // ...
    if (_uiManager != null)
    {
        var uPlayer = UnturnedPlayer.FromPlayer(player);
        _uiManager.CloseUI(uPlayer);
    }
    EffectManager.sendUIEffect(definition.EffectId, /* ... */);
    // ...
}
```

Pass the `UIManager` into `MiniGameManager`'s constructor for this behaviour to kick in:

```csharp
var mini = new MiniGameManager(uiManager);
```

If no `UIManager` is supplied, the mini-game still runs but the caller is responsible for making sure no UI is open that could fight over input events.

## Manual completion

A handler signals a terminal state by calling `MiniGameManager.Complete`:

```csharp
MyPlugin.MiniGames.Complete(instance, MiniGameState.Succeeded);
```

`Complete` is idempotent against the timeout race -- if the tick runner has already scheduled an auto-timeout in the same frame, a subsequent `Complete` with `TimedOut` is ignored. Other results always take effect.

## Cancellation

Two paths lead to `Cancelled`:

1. External `Cancel(player)` call. Use this when the player walks away, the item triggering the mini-game is destroyed, or admin tooling stops the session.
2. `MiniGameManager.Unload()` during plugin shutdown. Every still-running instance is cancelled so `OnEnd` fires before the host GameObject is destroyed.

After cancellation the `MiniGameCompleted` event fires with `State == Cancelled`, so external systems can distinguish a clean cancel from a real failure.

## Event ordering guarantees

- `OnStart` always fires before any `OnTick`.
- `OnEnd` always fires before `MiniGameCompleted`.
- `OnTick` never fires after `OnEnd`.
- `OnInput` is not delivered to a terminal instance (the runner is removed from `_active` as soon as the state leaves `Running`).

## Thread affinity

Every callback runs on the main Unity thread. `OnTick` fires from Unity's `Update`; `OnInput` fires from Unturned's `EffectManager` events which are also main-thread. Handlers can call `ThreadHelper.RunAsynchronously` if they need to offload work (database lookups, etc.) but the callback itself must not block.
