using BlueBeard.Core.Configs;

namespace BlueBeard.SnapLogic.Config;

/// <summary>
/// Default configuration for the SnapLogic system.
/// </summary>
public class SnapLogicConfig : IConfig
{
    /// <summary>
    /// Default snap detection radius when not specified by a <see cref="Models.SnapDefinition"/>.
    /// </summary>
    public float DefaultSnapRadius { get; set; }

    /// <summary>
    /// Whether to automatically register host barricades when they are placed.
    /// </summary>
    public bool AutoRegisterHosts { get; set; }

    /// <summary>
    /// Whether to destroy all snapped children when a host barricade is destroyed.
    /// </summary>
    public bool DestroyChildrenWithHost { get; set; }

    public void LoadDefaults()
    {
        DefaultSnapRadius = 2.0f;
        AutoRegisterHosts = true;
        DestroyChildrenWithHost = true;
    }
}
