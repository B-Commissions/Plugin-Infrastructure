using BlueBeard.Core.Configs;

namespace BlueBeard.Zones.Config;

public class ZonesConfig : IConfig
{
    public string StorageType { get; set; }
    public bool EnableFlagEnforcement { get; set; }

    public void LoadDefaults()
    {
        StorageType = "json";
        EnableFlagEnforcement = true;
    }
}
