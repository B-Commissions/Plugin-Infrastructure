namespace BlueBeard.SnapLogic;

/// <summary>
/// Why a snap host left the registry — carried by <see cref="SnapManager.OnHostRemoved"/>.
/// </summary>
public enum HostRemovalReason
{
    /// <summary>The host barricade was destroyed in the world (gunfire, explosion, decay).</summary>
    Destroyed,

    /// <summary>A player salvaged the host barricade.</summary>
    Salvaged,

    /// <summary>The definition was unregistered via <see cref="SnapManager.UnregisterDefinition"/>.</summary>
    Unregistered
}
