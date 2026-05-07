# Abstractions

The framework-agnostic contracts live in `BlueBeard.Core.Abstractions`. Each one has a Rocket adapter (in `BlueBeard.RocketMod`) and an OpenMod adapter (in `BlueBeard.OpenMod`).

## BlueBeardHost

Static service locator. Configured once at plugin load via `RocketModBootstrap.Install()` or `OpenModBootstrap.Install()`. Read-only afterwards.

```csharp
public static class BlueBeardHost
{
    public static ILogger Logger { get; }
    public static IChat Chat { get; }
    public static IPermissions Permissions { get; }
    public static ITaskDispatcher Dispatcher { get; }
    public static IPlayerEvents PlayerEvents { get; }

    public static bool IsConfigured { get; }

    public static void Configure(
        ILogger logger = null,
        IChat chat = null,
        IPermissions permissions = null,
        ITaskDispatcher dispatcher = null,
        IPlayerEvents playerEvents = null);

    public static void Reset();
}
```

Accessing a property before configuration throws `InvalidOperationException`.

## ILogger

```csharp
public interface ILogger
{
    void Log(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogException(Exception exception, string context = null);
}
```

| Adapter | Backed by |
|---------|-----------|
| RocketMod | `Rocket.Core.Logging.Logger` static methods |
| OpenMod | `Microsoft.Extensions.Logging.ILogger` injected by DI |

## IChat

```csharp
public interface IChat
{
    void Say(IPlayer player, string message, Color color = default);
    void Say(CSteamID steamId, string message, Color color = default);
    void Broadcast(string message, Color color = default);
}
```

| Adapter | Backed by |
|---------|-----------|
| RocketMod | `UnturnedChat.Say` |
| OpenMod | `ChatManager.serverSendMessage` (SDG, via `IUnturnedUserDirectory` for user lookup) |

## IPermissions

```csharp
public interface IPermissions
{
    bool HasPermission(IPlayer player, string permission);
    bool HasPermission(CSteamID steamId, string permission);
    void AddPlayerToGroup(string groupName, IPlayer player);
    void RemovePlayerFromGroup(string groupName, IPlayer player);
}
```

| Adapter | Backed by |
|---------|-----------|
| RocketMod | `R.Permissions.AddPlayerToGroup`, `IRocketPlayer.HasPermission` |
| OpenMod | `IPermissionChecker.CheckPermissionAsync` + `IPermissionRoleStore.AddRoleToActorAsync` (called sync via `.GetAwaiter().GetResult()`) |

**Permission default differs between hosts.** RocketMod returns true for unset nodes; OpenMod returns `PermissionGrantResult.Default` which the adapter maps to `false`. Register every required permission explicitly when running under OpenMod.

## ITaskDispatcher

```csharp
public interface ITaskDispatcher
{
    void QueueOnMainThread(Action action, float delaySeconds = 0);
}
```

| Adapter | Backed by |
|---------|-----------|
| RocketMod | `Rocket.Core.Utils.TaskDispatcher.QueueOnMainThread` |
| OpenMod | A small Unity coroutine runner (no main-thread dispatcher in OpenMod itself) |

## IPlayerEvents

```csharp
public interface IPlayerEvents
{
    event Action<IPlayer> PlayerConnected;
    event Action<IPlayer> PlayerDisconnected;
}
```

| Adapter | Backed by |
|---------|-----------|
| RocketMod | `U.Events.OnPlayerConnected` / `OnPlayerDisconnected` |
| OpenMod | `UnturnedUserConnectedEvent` / `UnturnedUserDisconnectedEvent` via `IEventBus` |

## IPlayer

```csharp
public interface IPlayer
{
    CSteamID SteamId { get; }
    string DisplayName { get; }
    Vector3 Position { get; }
    bool IsConsole { get; }
    Player Unturned { get; }                    // null for console
    bool HasPermission(string permission);
    void SendMessage(string message, Color color = default);
}
```

Concrete wrappers:

| Adapter | Wrapper class | Factory |
|---------|---------------|---------|
| RocketMod | `RocketPlayer` | `RocketPlayer.From(unturnedPlayer)` / `RocketPlayer.From(sdgPlayer)` / `RocketPlayer.Console` |
| OpenMod | `OpenModPlayer` | `OpenModPlayer.From(unturnedUser)` / `OpenModPlayer.Console` |

## ICommand / ICommandContext

```csharp
public interface ICommand
{
    string Name { get; }
    string Help { get; }
    string Syntax { get; }
    IReadOnlyList<string> Aliases { get; }
    IReadOnlyList<string> Permissions { get; }
    bool AllowConsole { get; }
    bool AllowPlayer { get; }
    Task ExecuteAsync(ICommandContext context);
}

public interface ICommandContext
{
    IPlayer Caller { get; }
    string CommandName { get; }
    IReadOnlyList<string> Args { get; }
}
```

These are reserved for the next migration step — once `CommandBase` is rewritten on top of `ICommand`, both adapters will register the same `ICommand` instances with their respective host's command system. Today, `BlueBeard.Core/Commands/CommandBase` still extends `IRocketCommand` directly.

## IPluginHost

```csharp
public interface IPluginHost
{
    string Name { get; }
    string Directory { get; }
    IReadOnlyList<ICommand> Commands { get; }
    void Load();
    void Unload();
}
```

Reserved for the per-library migration: existing plugin entry points (e.g. `ZonesPlugin : RocketPlugin`) will be split into a framework-agnostic module that implements `IPluginHost` and a thin Rocket / OpenMod shell that hosts it.
