using System;
using System.Collections.Generic;
using System.Threading;

namespace BlueBeard.Events;

/// <summary>
/// Non-generic base surface so <see cref="EventBusManager"/> can reach common operations
/// (currently just <see cref="Clear"/>) without knowing the concrete action type.
/// </summary>
public interface IEventBus
{
    void Clear();
    int SubscriberCount { get; }
}

/// <summary>
/// A generic typed event bus scoped to a single <see cref="Enum"/>-typed action namespace.
/// Subscribers register interest in one or more actions via a bitmask (<typeparamref name="TAction"/>
/// should be decorated with <see cref="FlagsAttribute"/> for masking to work as intended).
///
/// Dispatch is synchronous on the calling thread. Exceptions from subscribers are NOT caught
/// by the bus — wrap <see cref="Publish"/> if your domain needs exception isolation.
///
/// The subscriber list is snapshotted before iteration so that handlers adding or removing
/// subscriptions during dispatch do not invalidate the enumeration.
/// </summary>
public class EventBus<TAction> : IEventBus where TAction : struct, Enum
{
    private readonly List<Entry> _entries = new();
    private long _nextId;

    private struct Entry
    {
        public long Id;
        public long Mask;
        public Action<TAction, EventContext<TAction>> Handler;
    }

    /// <summary>
    /// Subscribe a handler to one or more actions. Returns a <see cref="Subscription"/>
    /// handle that can be passed to <see cref="Unsubscribe"/>.
    /// </summary>
    public Subscription Subscribe(TAction mask, Action<TAction, EventContext<TAction>> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        var id = Interlocked.Increment(ref _nextId);
        _entries.Add(new Entry
        {
            Id = id,
            Mask = Convert.ToInt64(mask),
            Handler = handler,
        });
        return new Subscription(id);
    }

    /// <summary>Remove a previously registered subscription.</summary>
    public void Unsubscribe(Subscription subscription)
    {
        if (subscription == null) return;
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            if (_entries[i].Id == subscription.Id)
            {
                _entries.RemoveAt(i);
                return;
            }
        }
    }

    /// <summary>
    /// Fire an event. Every subscriber whose mask has any bit in common with
    /// <paramref name="action"/> will receive the callback. The action value and
    /// <paramref name="context"/> are passed unchanged.
    /// </summary>
    public void Publish(TAction action, EventContext<TAction> context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        context.Action = action;
        var actionBits = Convert.ToInt64(action);

        // Snapshot to allow subscribers to modify the list during dispatch.
        var snapshot = _entries.ToArray();
        foreach (var entry in snapshot)
        {
            if ((entry.Mask & actionBits) != 0)
                entry.Handler(action, context);
        }
    }

    /// <summary>Remove all subscriptions.</summary>
    public void Clear()
    {
        _entries.Clear();
    }

    /// <summary>Current number of registered subscribers (diagnostics / testing).</summary>
    public int SubscriberCount => _entries.Count;
}
