# Getting Started

## Installation

Add a project reference in your plugin's `.csproj`:

```xml
<ProjectReference Include="..\BlueBeard.MiniGames\BlueBeard.MiniGames.csproj" />
```

BlueBeard.MiniGames depends on `BlueBeard.Core` and `BlueBeard.UI` (the latter is used purely for the `UIManager.CloseUI` coordination; mini-games themselves are NOT part of the `IUI` hierarchy).

## Core Concepts

### Definition
`MiniGameDefinition` is the static configuration for a mini-game type: the `Id`, the Unturned effect asset id, the duration in seconds, an `AllowRetry` flag, and a parameter bag. Definitions are reusable -- one definition can be started many times for different players.

### Handler
`IMiniGameHandler` is the implementation contract. The framework calls `OnStart`, `OnTick`, `OnInput`, and `OnEnd` at the appropriate points in the lifecycle. Register one handler per `Id`.

### Instance
`MiniGameInstance` represents a single in-progress session for a specific player. It carries the player reference, the definition, a countdown `TimeRemaining`, the current `MiniGameState`, and a `SessionData` dictionary for handler-specific scratch storage.

### Independent of BlueBeard.UI
Mini-games are standalone overlays, **not** part of the `IUI / IUIScreen / IUIDialog` hierarchy. Starting a mini-game closes any open `IUI` for the player so input events from `EffectManager` aren't delivered to both systems. See [Lifecycle](Lifecycle.md) for details.

## Basic Setup

```csharp
using BlueBeard.MiniGames;
using BlueBeard.UI;

public class MyPlugin : RocketPlugin
{
    public static UIManager UI { get; private set; }
    public static MiniGameManager MiniGames { get; private set; }

    protected override void Load()
    {
        UI = new UIManager();
        UI.Load();

        MiniGames = new MiniGameManager(UI);          // pass the UIManager so Start can CloseUI
        MiniGames.RegisterHandler("hotwire", new HotwireHandler());
        MiniGames.Load();
    }

    protected override void Unload()
    {
        MiniGames.Unload();
        UI.Unload();
    }
}
```

## Writing a Handler

```csharp
public class HotwireHandler : IMiniGameHandler
{
    public void OnStart(MiniGameInstance i)
    {
        // Randomise wire colours, send UI text via EffectManager.sendUIEffectText, etc.
        i.SessionData["correctWire"] = new Random().Next(3);
    }

    public void OnTick(MiniGameInstance i, float dt)
    {
        // Update the on-screen timer display each frame.
        var connection = i.Player.channel.owner.transportConnection;
        EffectManager.sendUIEffectText((short)i.Definition.EffectId, connection, true,
            "Canvas/Timer", $"{i.TimeRemaining:F1}s");
    }

    public void OnInput(MiniGameInstance i, string inputName, string value)
    {
        if (!inputName.StartsWith("Wire_")) return;
        var wireIndex = int.Parse(inputName.Substring(5));
        var correct = (int)i.SessionData["correctWire"];

        if (wireIndex == correct)
            MyPlugin.MiniGames.Complete(i, MiniGameState.Succeeded);
        else
            MyPlugin.MiniGames.Complete(i, MiniGameState.Failed);
    }

    public void OnEnd(MiniGameInstance i)
    {
        // Clean up any handler-specific UI state.
    }
}
```

## Starting a Mini-Game

```csharp
var definition = new MiniGameDefinition
{
    Id = "hotwire",
    EffectId = 50710,
    Duration = 8f,
    AllowRetry = false,
};

var instance = MyPlugin.MiniGames.Start(player.Player, definition);
// instance.State is Running at this point; will transition to Succeeded / Failed / TimedOut / Cancelled
```

`Start` synchronously sends the effect, creates the instance, attaches the tick runner, and calls `OnStart`. It returns the instance so the caller can stash it if it needs to track the session externally.

## Reacting to Completion

```csharp
MyPlugin.MiniGames.MiniGameCompleted += instance =>
{
    if (instance.State == MiniGameState.Succeeded)
        UnlockDoor(instance.Player);
    else if (instance.State == MiniGameState.Failed || instance.State == MiniGameState.TimedOut)
        TriggerAlarm(instance.Player);
};
```

The event fires AFTER `OnEnd` has run, so any state cleanup the handler does is already complete.

## Quick Reference

| API | Purpose |
|-----|---------|
| `MiniGameManager.RegisterHandler(id, handler)` | Attach a handler for a mini-game type |
| `MiniGameManager.Start(player, definition)` | Begin a session; cancels any previous session for the same player |
| `MiniGameManager.Cancel(player)` | Cancel the player's active mini-game (state -> `Cancelled`) |
| `MiniGameManager.Complete(instance, result)` | Handler-driven completion (success/failure/custom result) |
| `MiniGameManager.MiniGameCompleted` | Event raised after `OnEnd` for any terminal state |
