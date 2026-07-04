using System;

namespace BlueBeard.Core.Abstractions;

/// <summary>
/// Ambient service-locator for the active mod-host adapter. A plugin's bootstrap calls
/// <see cref="Configure"/> at load time to install the RocketMod or OpenMod adapter,
/// then BlueBeard libraries access services through the static accessors.
///
/// This indirection is what lets BlueBeard libraries ship a single binary that runs
/// under either framework: nothing in <c>BlueBeard.Core</c> references RocketMod or
/// OpenMod directly — the adapter does, and is installed at runtime.
/// </summary>
public static class BlueBeardHost
{
    private static ILogger _logger;
    private static IChat _chat;
    private static IPermissions _permissions;
    private static ITaskDispatcher _dispatcher;
    private static IPlayerEvents _playerEvents;
    private static IPermissionsAsync _permissionsAsync;
    private static ITranslations _translations;
    private static readonly ITranslations PassthroughTranslator = new PassthroughTranslations();

    public static ILogger Logger
        => _logger ?? throw NotInitialized(nameof(ILogger));

    public static IChat Chat
        => _chat ?? throw NotInitialized(nameof(IChat));

    public static IPermissions Permissions
        => _permissions ?? throw NotInitialized(nameof(IPermissions));

    public static ITaskDispatcher Dispatcher
        => _dispatcher ?? throw NotInitialized(nameof(ITaskDispatcher));

    public static IPlayerEvents PlayerEvents
        => _playerEvents ?? throw NotInitialized(nameof(IPlayerEvents));

    /// <summary>
    /// Async permissions. Natively async under OpenMod; wraps the synchronous
    /// <see cref="Permissions"/> when the adapter installs no dedicated implementation.
    /// </summary>
    public static IPermissionsAsync PermissionsAsync
        => _permissionsAsync ??= new SyncPermissionsAsyncWrapper(Permissions);

    /// <summary>
    /// Message translation. Never null — falls back to a passthrough (key + formatting)
    /// when the adapter installs no translator, so callers need no null checks.
    /// </summary>
    public static ITranslations Translations
        => _translations ?? PassthroughTranslator;

    public static bool IsConfigured =>
        _logger != null || _chat != null || _permissions != null || _dispatcher != null || _playerEvents != null;

    /// <summary>
    /// Install one or more adapter implementations. Pass null for any service you don't
    /// want to override; subsequent calls overwrite previously-set instances.
    /// </summary>
    public static void Configure(
        ILogger logger = null,
        IChat chat = null,
        IPermissions permissions = null,
        ITaskDispatcher dispatcher = null,
        IPlayerEvents playerEvents = null)
    {
        if (logger != null) _logger = logger;
        if (chat != null) _chat = chat;
        if (permissions != null) _permissions = permissions;
        if (dispatcher != null) _dispatcher = dispatcher;
        if (playerEvents != null) _playerEvents = playerEvents;
    }

    /// <summary>
    /// Install the optional extended services (kept out of <see cref="Configure"/> so the
    /// original signature stays binary-stable for already-compiled bootstraps).
    /// </summary>
    public static void ConfigureExtras(
        IPermissionsAsync permissionsAsync = null,
        ITranslations translations = null)
    {
        if (permissionsAsync != null) _permissionsAsync = permissionsAsync;
        if (translations != null) _translations = translations;
    }

    /// <summary>Clear all installed services. Intended for plugin reloads and tests.</summary>
    public static void Reset()
    {
        _logger = null;
        _chat = null;
        _permissions = null;
        _dispatcher = null;
        _playerEvents = null;
        _permissionsAsync = null;
        _translations = null;
    }

    private static InvalidOperationException NotInitialized(string serviceName) =>
        new($"BlueBeardHost.{serviceName} has not been configured. " +
            "Call BlueBeard.RocketMod.RocketModBootstrap.Install() or " +
            "BlueBeard.OpenMod.OpenModBootstrap.Install() at plugin load.");
}
