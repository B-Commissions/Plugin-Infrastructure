# BlueBeard.MiniGames

BlueBeard.MiniGames is a framework for building timed, interactive mini-games inside Unturned plugins. It handles the full lifecycle -- start, tick, input, end -- so implementations only define the game logic and visuals.

## Features

- **Definition + instance split** -- One `MiniGameDefinition` declares the effect asset, duration, and parameters; each `Start` call produces a fresh `MiniGameInstance` for the target player.
- **Handler interface** -- `IMiniGameHandler` exposes `OnStart`, `OnTick`, `OnInput`, `OnEnd`. Implementations focus on game logic, not plumbing.
- **Automatic tick** -- A `MonoBehaviour` runner drives `OnTick` every frame and auto-completes with `TimedOut` when the duration elapses.
- **Input routing** -- Hooks `EffectManager.onEffectButtonClicked` and `onEffectTextCommitted`; only the active player's handler receives the event.
- **One active per player** -- Starting a new mini-game cancels any previous one for the same player.
- **UI precedence** -- Starting a mini-game closes any open `BlueBeard.UI` for the player so input events aren't double-handled.
- **Completion event** -- `MiniGameCompleted` fires after `OnEnd` so external systems can react to outcomes without polling.

## Documentation

| Page | Description |
|------|-------------|
| [Getting Started](Getting-Started.md) | Installation, writing a handler, starting a mini-game |
| [Definitions and Handlers](Definitions-and-Handlers.md) | `MiniGameDefinition`, `MiniGameInstance`, `IMiniGameHandler` reference |
| [Lifecycle](Lifecycle.md) | State transitions, tick / input / end, UI precedence rule |
| [Examples](Examples.md) | Hotwire, reaction test, colour-match |

## Source Classes

| Class / Interface | Role |
|-------------------|------|
| `MiniGameManager` | `IManager` that owns handlers and active instances, wires input, raises `MiniGameCompleted` |
| `MiniGameDefinition` | Static configuration (id, effect, duration, parameters) |
| `MiniGameInstance` | Runtime state for a single player's session |
| `MiniGameState` | Enum: `Running`, `Succeeded`, `Failed`, `TimedOut`, `Cancelled` |
| `IMiniGameHandler` | Implementation contract for a specific mini-game type |
| `MiniGameTickRunner` | Internal `MonoBehaviour` that drives the per-instance tick |
