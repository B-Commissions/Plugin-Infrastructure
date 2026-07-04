using BlueBeard.Core.Abstractions;
using Rocket.API;

namespace BlueBeard.RocketMod;

/// <summary>
/// Single-call setup for plugins running under RocketMod. Call <see cref="Install"/> from
/// your plugin's <c>Load()</c> before using any BlueBeard library that depends on
/// <see cref="BlueBeardHost"/> services. Call <see cref="Uninstall"/> from <c>Unload()</c>.
/// </summary>
public static class RocketModBootstrap
{
    private static RocketPlayerEvents _playerEvents;

    public static void Install()
    {
        _playerEvents ??= new RocketPlayerEvents();
        BlueBeardHost.Configure(
            logger: new RocketLogger(),
            chat: new RocketChat(),
            permissions: new RocketPermissions(),
            dispatcher: new RocketTaskDispatcher(),
            playerEvents: _playerEvents);
    }

    /// <summary>
    /// Install with the plugin's Rocket translations bound to
    /// <see cref="BlueBeardHost.Translations"/>.
    /// </summary>
    public static void Install(IRocketPlugin plugin)
    {
        Install();
        InstallTranslations(plugin);
    }

    /// <summary>
    /// Bind a plugin's Rocket <c>Translations</c> to <see cref="BlueBeardHost.Translations"/>.
    /// </summary>
    public static void InstallTranslations(IRocketPlugin plugin)
    {
        if (plugin == null) return;
        BlueBeardHost.ConfigureExtras(translations: new RocketTranslations(plugin));
    }

    public static void Uninstall()
    {
        _playerEvents?.Dispose();
        _playerEvents = null;
        BlueBeardHost.Reset();
    }
}
