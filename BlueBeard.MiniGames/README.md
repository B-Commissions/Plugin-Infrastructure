# BlueBeard.MiniGames

Framework for building timed, interactive mini-games in Unturned plugins. Handles the full lifecycle — start, tick, input, end — so implementations only define the game logic and visuals.

## Features

- `MiniGameDefinition` — configure the effect id, duration, parameters
- `IMiniGameHandler` — implement `OnStart`, `OnTick`, `OnInput`, `OnEnd`
- `MiniGameManager` — one active mini-game per player; automatic timeout; raises `MiniGameCompleted`
- Standalone overlays — mini-games do NOT live inside the `IUI` hierarchy; starting one closes any open UI (mini-game wins input precedence)
- Input routing — hooks `EffectManager.onEffectButtonClicked` / `onEffectTextCommitted` and delivers only to the handler of the player's active instance

## Quick example

```csharp
public class HotwireHandler : IMiniGameHandler
{
    public void OnStart(MiniGameInstance i) { /* set up UI */ }
    public void OnTick(MiniGameInstance i, float dt) { /* update timer text */ }
    public void OnInput(MiniGameInstance i, string button, string value)
    {
        if (button == "Wire_Red") manager.Complete(i, MiniGameState.Succeeded);
        if (button == "Wire_Blue") manager.Complete(i, MiniGameState.Failed);
    }
    public void OnEnd(MiniGameInstance i) { /* clear UI */ }
}

manager.RegisterHandler("hotwire", new HotwireHandler());

var def = new MiniGameDefinition
{
    Id = "hotwire",
    EffectId = 50710,
    Duration = 8f,
};
manager.Start(player, def);
```

See `docs/MiniGames/` in the Infrastructure repo for full reference.
