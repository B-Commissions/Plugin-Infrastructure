using BlueBeard.Core.Abstractions;
using OpenMod.API;
using OpenMod.API.Eventing;
using OpenMod.API.Permissions;
using OpenMod.API.Users;
using OpenMod.Core.Permissions;
using OpenMod.Unturned.Users;
using MsLogger = Microsoft.Extensions.Logging.ILogger;

namespace BlueBeard.OpenMod;

/// <summary>
/// Single-call setup for plugins running under OpenMod. Resolve the required services
/// from your plugin's DI container (typically in <c>OnLoadAsync</c>) and pass them to
/// <see cref="Install"/>. Call <see cref="Uninstall"/> from <c>OnUnloadAsync</c>.
/// </summary>
public static class OpenModBootstrap
{
    private static OpenModPlayerEvents _playerEvents;

    public static void Install(
        MsLogger logger,
        IPermissionChecker permissionChecker,
        IPermissionRoleStore permissionRoleStore,
        IUnturnedUserDirectory userDirectory,
        IEventBus eventBus,
        IOpenModComponent component)
    {
        _playerEvents ??= new OpenModPlayerEvents(eventBus, component);

        // Install runs on the main thread (OpenMod plugin load), the only place the
        // dispatcher's runner GameObject can legally be created. After this,
        // QueueOnMainThread is safe from any thread.
        OpenModTaskDispatcher.InitializeRunner();

        BlueBeardHost.Configure(
            logger: new OpenModLogger(logger),
            chat: new OpenModChat(userDirectory),
            permissions: new OpenModPermissions(permissionChecker, permissionRoleStore, userDirectory),
            dispatcher: new OpenModTaskDispatcher(),
            playerEvents: _playerEvents);
    }

    public static void Uninstall()
    {
        _playerEvents?.Dispose();
        _playerEvents = null;
        OpenModTaskDispatcher.DestroyRunner();
        BlueBeardHost.Reset();
    }
}
