using System;
using BlueBeard.Core.Configs;
using BlueBeard.Core.Helpers;
using BlueBeard.Database;
using BlueBeard.Zones.BlockLists;
using BlueBeard.Zones.Builder;
using BlueBeard.Zones.Config;
using BlueBeard.Zones.Flags;
using BlueBeard.Zones.Storage;
using BlueBeard.Zones.Tracking;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using SDG.Unturned;

namespace BlueBeard.Zones;

public class ZonesPlugin : RocketPlugin
{
    public static ZonesPlugin Instance { get; private set; }
    public ConfigManager ConfigManager { get; private set; }
    public ZoneManager ZoneManager { get; private set; }
    public BlockListManager BlockListManager { get; private set; }
    public PlayerTracker PlayerTracker { get; private set; }
    public ZoneBuilderManager ZoneBuilderManager { get; private set; }
    public FlagEnforcementManager FlagEnforcement { get; private set; }

    private IZoneRepository _repository;

    protected override void Load()
    {
        Instance = this;

        ConfigManager = new ConfigManager();
        ConfigManager.Initialize(Directory);
        ConfigManager.LoadConfig<ZonesConfig>();

        var config = ConfigManager.GetConfig<ZonesConfig>();

        // Create repository based on config
        if (config.StorageType.Equals("mysql", StringComparison.OrdinalIgnoreCase))
        {
            ConfigManager.LoadConfig<DatabaseConfig>();
            var db = new DatabaseManager();
            db.Initialize(ConfigManager);
            db.Load();
            _repository = new MySqlZoneRepository(db);
        }
        else
        {
            _repository = new JsonZoneRepository(Directory);
        }

        // Initialize managers
        ZoneManager = new ZoneManager();
        ZoneManager.Initialize(_repository);

        BlockListManager = new BlockListManager();
        BlockListManager.Initialize(_repository);

        PlayerTracker = new PlayerTracker();
        PlayerTracker.Initialize(ZoneManager);

        ZoneBuilderManager = new ZoneBuilderManager();
        ZoneBuilderManager.Initialize(ZoneManager);

        if (config.EnableFlagEnforcement)
        {
            FlagEnforcement = new FlagEnforcementManager();
            FlagEnforcement.Initialize(ZoneManager, PlayerTracker, BlockListManager);
        }

        Level.onLevelLoaded += OnLevelLoaded;

        CommandDocGenerator.Generate(Directory);
        Logger.Log("[BlueBeard.Zones] Plugin loaded.");
    }

    private void OnLevelLoaded(int level)
    {
        ZoneManager.Load();
        BlockListManager.Load();
        PlayerTracker.Load();
        FlagEnforcement?.Load();
    }

    protected override void Unload()
    {
        Level.onLevelLoaded -= OnLevelLoaded;

        FlagEnforcement?.Unload();
        PlayerTracker?.Unload();
        ZoneBuilderManager?.Unload();
        BlockListManager?.Unload();
        ZoneManager?.Unload();

        Logger.Log("[BlueBeard.Zones] Plugin unloaded.");
    }
}
