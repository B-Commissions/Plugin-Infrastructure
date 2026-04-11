# Examples

## Hotwire (cut the right wire)

Classic "cut one of N wires" game. On start, pick a random correct wire. The player clicks `Wire_0`, `Wire_1`, etc. Correct wire wins, wrong wire fails, timeout auto-fails.

```csharp
using System;
using BlueBeard.MiniGames;
using SDG.Unturned;

public class HotwireHandler : IMiniGameHandler
{
    private static readonly Random Rng = new();

    public void OnStart(MiniGameInstance i)
    {
        var wireCount = i.Definition.Parameters.TryGetValue("wireCount", out var wc) ? (int)wc : 3;
        i.SessionData["correct"] = Rng.Next(wireCount);
        i.SessionData["wireCount"] = wireCount;

        var conn = i.Player.channel.owner.transportConnection;
        EffectManager.sendUIEffectText((short)i.Definition.EffectId, conn, true,
            "Canvas/Prompt", $"Cut the correct wire ({wireCount} options).");
    }

    public void OnTick(MiniGameInstance i, float dt)
    {
        var conn = i.Player.channel.owner.transportConnection;
        EffectManager.sendUIEffectText((short)i.Definition.EffectId, conn, true,
            "Canvas/Timer", $"{i.TimeRemaining:F1}s");
    }

    public void OnInput(MiniGameInstance i, string inputName, string value)
    {
        if (!inputName.StartsWith("Wire_")) return;
        if (!int.TryParse(inputName.Substring(5), out var wire)) return;

        var correct = (int)i.SessionData["correct"];
        var result = wire == correct ? MiniGameState.Succeeded : MiniGameState.Failed;
        MyPlugin.MiniGames.Complete(i, result);
    }

    public void OnEnd(MiniGameInstance i)
    {
        var conn = i.Player.channel.owner.transportConnection;
        var message = i.State switch
        {
            MiniGameState.Succeeded => "Wired!",
            MiniGameState.Failed    => "Triggered the alarm.",
            MiniGameState.TimedOut  => "Out of time.",
            _                        => "Cancelled.",
        };
        EffectManager.sendUIEffectText((short)i.Definition.EffectId, conn, true, "Canvas/Result", message);
    }
}
```

Trigger it from a command:

```csharp
var def = new MiniGameDefinition { Id = "hotwire", EffectId = 50710, Duration = 8f };
def.Parameters["wireCount"] = 3;
MyPlugin.MiniGames.Start(player.Player, def);
```

And react to the outcome:

```csharp
MyPlugin.MiniGames.MiniGameCompleted += instance =>
{
    if (instance.Definition.Id != "hotwire") return;
    if (instance.State == MiniGameState.Succeeded)
        UnlockDoor(instance.Player);
    else if (instance.State == MiniGameState.Failed || instance.State == MiniGameState.TimedOut)
        TriggerAlarm(instance.Player);
};
```

## Reaction test (hit the button when the indicator flashes)

Progress-bar style timing game: the on-screen indicator moves back and forth; the player hits `Stop` when the indicator is inside the target zone.

```csharp
public class ReactionHandler : IMiniGameHandler
{
    public void OnStart(MiniGameInstance i)
    {
        i.SessionData["pos"] = 0f;
        i.SessionData["dir"] = 1f;
    }

    public void OnTick(MiniGameInstance i, float dt)
    {
        var pos = (float)i.SessionData["pos"];
        var dir = (float)i.SessionData["dir"];

        pos += dir * dt * 0.8f;
        if (pos >= 1f) { pos = 1f; dir = -1f; }
        if (pos <= 0f) { pos = 0f; dir = 1f; }

        i.SessionData["pos"] = pos;
        i.SessionData["dir"] = dir;

        var conn = i.Player.channel.owner.transportConnection;
        EffectManager.sendUIEffectText((short)i.Definition.EffectId, conn, true,
            "Canvas/Indicator", new string('#', (int)(pos * 20)));
    }

    public void OnInput(MiniGameInstance i, string inputName, string value)
    {
        if (inputName != "Stop") return;
        var pos = (float)i.SessionData["pos"];
        var result = (pos >= 0.45f && pos <= 0.55f)
            ? MiniGameState.Succeeded
            : MiniGameState.Failed;
        MyPlugin.MiniGames.Complete(i, result);
    }

    public void OnEnd(MiniGameInstance i) { }
}
```

## Colour match (type the sequence)

Text-input style game: show a sequence of colours, the player must type them back in order.

```csharp
public class ColourMatchHandler : IMiniGameHandler
{
    private static readonly string[] Colours = { "red", "green", "blue", "yellow" };
    private static readonly Random Rng = new();

    public void OnStart(MiniGameInstance i)
    {
        var length = 4;
        var sequence = new string[length];
        for (var k = 0; k < length; k++) sequence[k] = Colours[Rng.Next(Colours.Length)];
        i.SessionData["sequence"] = string.Join(" ", sequence);

        var conn = i.Player.channel.owner.transportConnection;
        EffectManager.sendUIEffectText((short)i.Definition.EffectId, conn, true,
            "Canvas/Prompt", (string)i.SessionData["sequence"]);
    }

    public void OnTick(MiniGameInstance i, float dt) { }

    public void OnInput(MiniGameInstance i, string inputName, string value)
    {
        if (inputName != "Answer") return;
        var expected = (string)i.SessionData["sequence"];
        var normalised = (value ?? "").Trim().ToLowerInvariant();
        var result = normalised == expected ? MiniGameState.Succeeded : MiniGameState.Failed;
        MyPlugin.MiniGames.Complete(i, result);
    }

    public void OnEnd(MiniGameInstance i) { }
}
```
