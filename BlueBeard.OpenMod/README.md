# BlueBeard.OpenMod

OpenMod host adapter for [BlueBeard.Core](../BlueBeard.Core/README.md) abstractions. Implements `ILogger`, `IChat`, `IPermissions`, `ITaskDispatcher`, `IPlayer`, and `IPlayerEvents` against OpenMod / Unturned APIs.

## Usage

In your `OpenModUnturnedPlugin.OnLoadAsync`:

```csharp
using BlueBeard.OpenMod;
using BlueBeard.Core.Abstractions;
using Microsoft.Extensions.Logging;
using OpenMod.API.Eventing;
using OpenMod.API.Permissions;
using OpenMod.API.Users;
using OpenMod.Core.Permissions;
using OpenMod.Unturned.Plugins;
using OpenMod.Unturned.Users;

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
        IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = logger;
        _permissionChecker = permissionChecker;
        _permissionRoleStore = permissionRoleStore;
        _userDirectory = userDirectory;
        _eventBus = eventBus;
    }

    protected override async UniTask OnLoadAsync()
    {
        OpenModBootstrap.Install(
            logger: _logger,
            permissionChecker: _permissionChecker,
            permissionRoleStore: _permissionRoleStore,
            userDirectory: _userDirectory,
            eventBus: _eventBus,
            component: this);

        // Now BlueBeard libraries can use BlueBeardHost services freely:
        BlueBeardHost.Logger.Log("Hello from BlueBeard");
    }

    protected override async UniTask OnUnloadAsync()
    {
        OpenModBootstrap.Uninstall();
    }
}
```

## Async / sync seam

OpenMod's `IPermissionChecker.CheckPermissionAsync` is async; `IPermissions.HasPermission` is sync. The adapter blocks via `.GetAwaiter().GetResult()` — safe on the Unturned main thread because permission backends are typically in-memory or fast file-backed.

## Permission grant defaults

OpenMod returns `PermissionGrantResult.Default` for unconfigured nodes (deny-by-default), which `OpenModPermissions` translates to `false`. RocketMod's permissive default does **not** carry over — register every required permission in OpenMod explicitly.

## Wrapping a connected user

Convert an OpenMod `UnturnedUser` to the abstraction with `OpenModPlayer.From(user)`. Use `OpenModPlayer.Console` for the server console.
