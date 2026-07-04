# OpenMod Adapter

`BlueBeard.OpenMod` implements the [BlueBeard.Core abstractions](Abstractions.md) against OpenMod's APIs.

## Reference

```xml
<ProjectReference Include="..\BlueBeard.OpenMod\BlueBeard.OpenMod.csproj" />
```

The adapter pulls in (via NuGet):

- `OpenMod.Unturned` (transitively `OpenMod.API`, `OpenMod.Core`)
- `Microsoft.Extensions.Logging.Abstractions`
- `Microsoft.Extensions.DependencyInjection.Abstractions`

Plus the SDG `Libs/` references. No RocketMod assemblies are referenced.

## Bootstrap

OpenMod is DI-driven — your plugin constructor receives the services, and you forward them to `OpenModBootstrap.Install`:

```csharp
public class MyPlugin : OpenModUnturnedPlugin
{
    private readonly ILogger<MyPlugin> _logger;
    private readonly IPermissionChecker _permissionChecker;
    private readonly IPermissionRoleStore _permissionRoleStore;
    private readonly IUnturnedUserDirectory _userDirectory;
    private readonly IEventBus _eventBus;

    public MyPlugin(
        ILogger<MyPlugin> logger,
        IPermissionChecker permissionChecker,
        IPermissionRoleStore permissionRoleStore,
        IUnturnedUserDirectory userDirectory,
        IEventBus eventBus,
        IServiceProvider sp) : base(sp)
    {
        _logger = logger;
        _permissionChecker = permissionChecker;
        _permissionRoleStore = permissionRoleStore;
        _userDirectory = userDirectory;
        _eventBus = eventBus;
    }

    protected override UniTask OnLoadAsync()
    {
        OpenModBootstrap.Install(
            logger: _logger,
            permissionChecker: _permissionChecker,
            permissionRoleStore: _permissionRoleStore,
            userDirectory: _userDirectory,
            eventBus: _eventBus,
            component: this);
        return UniTask.CompletedTask;
    }

    protected override UniTask OnUnloadAsync()
    {
        OpenModBootstrap.Uninstall();
        return UniTask.CompletedTask;
    }
}
```

## What each adapter does

| Service | Class | Maps to |
|---------|-------|---------|
| `ILogger` | `OpenModLogger` | A wrapped `Microsoft.Extensions.Logging.ILogger`, calling `LogInformation` / `LogWarning` / `LogError` |
| `IChat` | `OpenModChat` | `ChatManager.serverSendMessage` (SDG), with user lookup via `IUnturnedUserDirectory` |
| `IPermissions` | `OpenModPermissions` | `IPermissionChecker.CheckPermissionAsync` + `IPermissionRoleStore.AddRoleToActorAsync` / `RemoveRoleFromActorAsync` |
| `ITaskDispatcher` | `OpenModTaskDispatcher` | A small Unity coroutine runner (OpenMod doesn't ship a one-shot main-thread queue) |
| `IPlayerEvents` | `OpenModPlayerEvents` | `UnturnedUserConnectedEvent` / `UnturnedUserDisconnectedEvent` via `IEventBus` |
| `IPlayer` | `OpenModPlayer` | Wraps `UnturnedUser` |

## Async / sync seam

OpenMod's permission API is async; `IPermissions` is sync. The adapter blocks via `.ConfigureAwait(false).GetAwaiter().GetResult()` — safe on the Unturned main thread because permission backends are typically in-memory or fast file-backed. If you wire a slow-IO backend (a database-backed `IPermissionStore`), expect the synchronous facade to stall — consider switching that path to direct OpenMod calls for those code sites.

## Permission defaults

OpenMod returns `PermissionGrantResult.Default` for unset nodes. The adapter maps `Default` -> `false`. **You must register every permission node** for it to grant — RocketMod's permissive default does not carry over.

```csharp
// In your OpenMod plugin's OnLoadAsync, before Install():
await _permissionRegistry.RegisterPermissionAsync(this, "myplugin.use", "Use my plugin", PermissionGrantResult.Deny);
```

## Threading

`OpenModTaskDispatcher` checks if you're already on Unturned's main thread (via `ThreadUtil.assertIsGameThread`) and runs the action inline if so. Otherwise it queues onto a hidden `MainThreadRunner` `MonoBehaviour` (created lazily, marked `DontDestroyOnLoad`) using a coroutine. Delay support uses `WaitForSeconds`.

## Reload safety

`OpenModBootstrap.Install()` is idempotent. The single `OpenModPlayerEvents` instance retains its `IEventBus` subscription across re-installs. `Uninstall()` disposes the subscription and clears `BlueBeardHost`.

## Wrapping an OpenMod user

```csharp
using BlueBeard.OpenMod;

IPlayer wrapped = OpenModPlayer.From(unturnedUser);
IPlayer console = OpenModPlayer.Console;
```

The underlying `UnturnedUser` stays accessible via `((OpenModPlayer)wrapped).User`; the SDG `Player` (if any) via `wrapped.Unturned`.

## Threading model

`QueueOnMainThread` is safe from any thread: `OpenModBootstrap.Install()` pre-creates a
runner GameObject on the main thread, and dispatch only enqueues into a thread-safe queue
drained in `Update()` (delays use unscaled time computed on the main thread). Calling the
dispatcher from a background thread before `Install()` throws — install first.
`Uninstall()` destroys the runner so hot-reloads don't stack orphans.

`IPlayerEvents` handlers are marshalled to the main thread, matching the RocketMod
adapter's contract.

## Async permissions

The synchronous `IPermissions` shim blocks on OpenMod's async API (sync-over-async) and
exists for source compatibility. Prefer `BlueBeardHost.PermissionsAsync` in async code —
the OpenMod adapter installs a natively async implementation automatically. Both resolve
**online players only** (via the connected-user directory).

## Translations

Bind your plugin's localizer once and use `BlueBeardHost.Translations` everywhere:

```csharp
OpenModBootstrap.Install(...);
OpenModBootstrap.InstallTranslations(m_StringLocalizer);
```
