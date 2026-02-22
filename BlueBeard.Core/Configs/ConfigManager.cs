using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Rocket.Core.Logging;
using SDG.Framework.IO.Deserialization;
using SDG.Framework.IO.Serialization;

namespace BlueBeard.Core.Configs;

public class ConfigManager : IManager
{
    private readonly Dictionary<Type, object> _configs = new();
    private string _directory;

    public void Initialize(string pluginDirectory)
    {
        _directory = Path.Combine(pluginDirectory, "Configs");
        if (!Directory.Exists(_directory))
            Directory.CreateDirectory(_directory);
    }

    public void Load()
    {
    }

    public void LoadConfig<T>() where T : IConfig, new()
    {
        var config = ReadConfig<T>();
        _configs[typeof(T)] = config;
        SaveConfig(config);
    }

    public T ReloadConfig<T>() where T : IConfig, new()
    {
        var config = ReadConfig<T>();
        _configs[typeof(T)] = config;
        SaveConfig(config);
        return config;
    }

    private T ReadConfig<T>() where T : IConfig, new()
    {
        var path = Path.Combine(_directory, $"{typeof(T).Name}.configuration.xml");
        if (!File.Exists(path))
        {
            var newConfig = new T();
            newConfig.LoadDefaults();
            return newConfig;
        }

        try
        {
            var config = new XMLDeserializer().deserialize<T>(path);
            if (config != null)
            {
                ValidateAndMigrate(config, path);
                return config;
            }
        }
        catch { }

        var fallbackConfig = new T();
        fallbackConfig.LoadDefaults();
        return fallbackConfig;
    }

    private void ValidateAndMigrate<T>(T config, string path) where T : IConfig, new()
    {
        var defaults = new T();
        defaults.LoadDefaults();

        HashSet<string> xmlElements;
        try
        {
            var doc = XDocument.Load(path);
            xmlElements = new HashSet<string>(
                doc.Root.Elements().Select(e => e.Name.LocalName));
        }
        catch
        {
            return;
        }

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .ToArray();

        var propertyNames = new HashSet<string>(properties.Select(p => p.Name));

        foreach (var prop in properties)
        {
            if (!xmlElements.Contains(prop.Name))
            {
                var defaultValue = prop.GetValue(defaults);
                prop.SetValue(config, defaultValue);
                Logger.LogWarning(
                    $"[ConfigManager] Property '{prop.Name}' missing from config file {Path.GetFileName(path)}. Using default value.");
            }
            else if (!prop.PropertyType.IsValueType)
            {
                var currentValue = prop.GetValue(config);
                var defaultValue = prop.GetValue(defaults);
                if (currentValue == null && defaultValue != null)
                {
                    prop.SetValue(config, defaultValue);
                    Logger.LogWarning(
                        $"[ConfigManager] Property '{prop.Name}' is null but default is non-null in {Path.GetFileName(path)}. Using default value.");
                }
            }
        }

        foreach (var element in xmlElements)
        {
            if (!propertyNames.Contains(element))
            {
                Logger.LogWarning(
                    $"[ConfigManager] Config file {Path.GetFileName(path)} contains unknown element '{element}'. It will be removed on save.");
            }
        }
    }

    public T GetConfig<T>() where T : IConfig, new()
    {
        if (_configs.TryGetValue(typeof(T), out var config))
        {
            return (T)config;
        }

        var newConfig = new T();
        newConfig.LoadDefaults();
        _configs[typeof(T)] = newConfig;
        return newConfig;
    }

    public void SaveConfig<T>(T config) where T : IConfig
    {
        var path = Path.Combine(_directory, $"{typeof(T).Name}.configuration.xml");
        if (!File.Exists(path))
        {
            File.Create(path).Close();
        }
        new XMLSerializer().serialize(config, path, true);
    }

    public void Unload()
    {
    }
}
