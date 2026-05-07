# Getting Started

Choose the adapter that matches your server, install it from your plugin's load entry point, and BlueBeard libraries that use `BlueBeardHost` services will route through it.

## RocketMod

```csharp
using BlueBeard.RocketMod;
using BlueBeard.Core.Abstractions;
using Rocket.Core.Plugins;

public class MyPlugin : RocketPlugin
{
    protected override void Load()
    {
        RocketModBootstrap.Install();

        BlueBeardHost.Logger.Log("BlueBeard host installed: RocketMod adapter");
    }

    protected override void Unload()
    {
        RocketModBootstrap.Uninstall();
    }
}
```

`RocketModBootstrap.Install()` configures all five services in one call. See [RocketMod Adapter](RocketMod-Adapter.md) for what each one does.

## OpenMod

```csharp
using BlueBeard.OpenMod;
using BlueBeard.Core.Abstractions;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenMod.API;
using OpenMod.API.Eventing;
using OpenMod.API.Permissions;
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

        BlueBeardHost.Logger.Log("BlueBeard host installed: OpenMod adapter");
    }

    protected override async UniTask OnUnloadAsync()
    {
        OpenModBootstrap.Uninstall();
    }
}
```

## After bootstrap

Both adapters configure exactly the same `BlueBeardHost` interface. From this point on, code that targets the abstraction looks identical regardless of host:

```csharp
BlueBeardHost.Logger.Log("hello");
BlueBeardHost.Chat.Broadcast("server is up", Color.green);

var actor = WrapPlayer(somePlayer); // RocketPlayer.From(...) or OpenModPlayer.From(...)
if (BlueBeardHost.Permissions.HasPermission(actor, "myplugin.use"))
{
    actor.SendMessage("Welcome!");
}
```

## Verifying the bootstrap ran

`BlueBeardHost.IsConfigured` returns true after a successful `Install()`. Accessing `BlueBeardHost.Logger` (or any other service) before configuration throws `InvalidOperationException` with a message pointing to the missing bootstrap call — handy for catching install-order bugs.

## Reload safety

Both `Install()` calls are idempotent — calling them again replaces the previously-installed services. Call `Uninstall()` from the unload path to clear the registrations and unsubscribe from player events.
