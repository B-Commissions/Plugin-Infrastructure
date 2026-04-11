using System;
using System.Collections.Generic;
using BlueBeard.Core;

namespace BlueBeard.Events;

/// <summary>
/// Registry of <see cref="EventBus{TAction}"/> instances keyed by the action enum type.
/// One manager per plugin; each distinct action enum resolves to its own bus.
/// </summary>
public class EventBusManager : IManager
{
    private readonly Dictionary<Type, IEventBus> _buses = new();

    /// <summary>
    /// Get the bus for a given action enum type, creating it if it does not yet exist.
    /// The same instance is returned on repeated calls for the same type.
    /// </summary>
    public EventBus<TAction> GetOrCreate<TAction>() where TAction : struct, Enum
    {
        var key = typeof(TAction);
        if (_buses.TryGetValue(key, out var existing))
            return (EventBus<TAction>)existing;

        var bus = new EventBus<TAction>();
        _buses[key] = bus;
        return bus;
    }

    public void Load()
    {
        // Lazy: buses are created on first GetOrCreate call. Nothing to do here.
    }

    public void Unload()
    {
        foreach (var bus in _buses.Values)
            bus.Clear();
        _buses.Clear();
    }
}
