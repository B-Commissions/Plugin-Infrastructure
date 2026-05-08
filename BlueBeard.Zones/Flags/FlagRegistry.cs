using System;
using System.Collections.Generic;
using Rocket.Core.Logging;

namespace BlueBeard.Zones.Flags;

public sealed class FlagInfo
{
    public string Name { get; }
    public string Description { get; }
    public IFlagHandler Handler { get; }
    public bool IsBuiltIn { get; }

    internal FlagInfo(string name, string description, IFlagHandler handler, bool isBuiltIn)
    {
        Name = name;
        Description = description;
        Handler = handler;
        IsBuiltIn = isBuiltIn;
    }
}

public sealed class FlagRegistry
{
    private readonly Dictionary<string, FlagInfo> _flags = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<FlagInfo> Flags => _flags.Values;

    public event Action<FlagInfo> HandlerRegistered;
    public event Action<FlagInfo> HandlerUnregistered;

    public bool TryGet(string name, out FlagInfo info) => _flags.TryGetValue(name, out info);

    public void RegisterFlag(string name, string description) =>
        Register(name, description, handler: null, isBuiltIn: false);

    public void RegisterHandler(IFlagHandler handler, string description)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        Register(handler.FlagName, description, handler, isBuiltIn: false);
    }

    internal void RegisterBuiltInHandler(IFlagHandler handler, string description)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        Register(handler.FlagName, description, handler, isBuiltIn: true);
    }

    public bool Unregister(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (!_flags.TryGetValue(name, out var info)) return false;

        _flags.Remove(name);
        if (info.Handler != null)
            HandlerUnregistered?.Invoke(info);
        return true;
    }

    private void Register(string name, string description, IFlagHandler handler, bool isBuiltIn)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Flag name must be non-empty.", nameof(name));

        if (_flags.ContainsKey(name))
        {
            Logger.LogWarning($"[BlueBeard.Zones] Flag '{name}' is already registered; ignoring duplicate registration.");
            return;
        }

        var info = new FlagInfo(name, description, handler, isBuiltIn);
        _flags.Add(name, info);

        if (handler != null)
            HandlerRegistered?.Invoke(info);
    }
}
