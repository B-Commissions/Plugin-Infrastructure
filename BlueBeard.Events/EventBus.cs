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

    /// <summary>Remove a previously registered subscription (used by <see cref="Subscription.Dispose"/>).</summary>
    void Unsubscribe(Subscription subscription);
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
    private readonly List<Entry> _entries = [];
    private readonly object _sync = new();
    private long _nextId;

    private struct Entry
    {
        public long Id;
        public long Mask;
        public int Priority;
        public Action<TAction, EventContext<TAction>> Handler;
    }

    /// <summary>
    /// Subscribe a handler to one or more actions. Returns a <see cref="Subscription"/>
    /// handle that can be passed to <see cref="Unsubscribe"/> or simply disposed.
    /// </summary>
    public Subscription Subscribe(TAction mask, Action<TAction, EventContext<TAction>> handler) =>
        Subscribe(mask, handler, priority: 0);

    /// <summary>
    /// Subscribe with an explicit priority: higher-priority handlers run first;
    /// equal priorities run in subscription order. The parameterless overload uses 0.
    /// </summary>
    public Subscription Subscribe(TAction mask, Action<TAction, EventContext<TAction>> handler, int priority)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        var id = Interlocked.Increment(ref _nextId);
        lock (_sync)
        {
            var entry = new Entry
            {
                Id = id,
                Mask = Convert.ToInt64(mask),
                Priority = priority,
                Handler = handler,
            };

            // Keep the list ordered (priority desc, then insertion order) so Publish's
            // snapshot needs no per-dispatch sort.
            var index = _entries.Count;
            while (index > 0 && _entries[index - 1].Priority < priority)
                index--;
            _entries.Insert(index, entry);
        }
        return new Subscription(id, this);
    }

    /// <summary>Remove a previously registered subscription.</summary>
    public void Unsubscribe(Subscription subscription)
    {
        if (subscription == null) return;
        lock (_sync)
        {
            for (var i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].Id == subscription.Id)
                {
                    _entries.RemoveAt(i);
                    return;
                }
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

        // Snapshot (under the lock) so subscribers modifying the list during dispatch —
        // or from another thread — never invalidate the enumeration. Handlers run
        // OUTSIDE the lock so reentrant Subscribe/Unsubscribe stays deadlock-free.
        Entry[] snapshot;
        lock (_sync)
        {
            snapshot = _entries.ToArray();
        }
        foreach (var entry in snapshot)
        {
            if ((entry.Mask & actionBits) != 0)
                entry.Handler(action, context);
        }
    }

    /// <summary>
    /// Publish and report whether any subscriber set <see cref="EventContext{TAction}.Cancelled"/> —
    /// removes the check-the-flag boilerplate at cancellable call sites:
    /// <code>
    /// if (bus.PublishCancelable(ShopAction.Purchase, ctx)) return; // a subscriber vetoed
    /// </code>
    /// </summary>
    public bool PublishCancelable(TAction action, EventContext<TAction> context)
    {
        Publish(action, context);
        return context.Cancelled;
    }

    /// <summary>Remove all subscriptions.</summary>
    public void Clear()
    {
        lock (_sync)
        {
            _entries.Clear();
        }
    }

    /// <summary>Current number of registered subscribers (diagnostics / testing).</summary>
    public int SubscriberCount
    {
        get { lock (_sync) return _entries.Count; }
    }
}
