using System;
using System.Collections.Generic;
using SDG.Unturned;

namespace BlueBeard.Events;

/// <summary>
/// Base context carried with every event dispatched through an <see cref="EventBus{TAction}"/>.
/// Subscribers read the action, any associated player, the UTC timestamp, and arbitrary
/// additional payload via <see cref="Data"/>. Subscribers may set <see cref="Cancelled"/>
/// to advise the publisher to abort the action; enforcement is the publisher's responsibility.
/// </summary>
public class EventContext<TAction> where TAction : struct, Enum
{
    /// <summary>The specific action that was raised. Populated by <see cref="EventBus{TAction}.Publish"/>.</summary>
    public TAction Action { get; internal set; }

    /// <summary>The Unturned player involved in the event, if any. Null for non-player-initiated events.</summary>
    public Player Player { get; set; }

    /// <summary>UTC timestamp at which the event was constructed.</summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>Arbitrary key-value payload for domain-specific data.</summary>
    public Dictionary<string, object> Data { get; } = new();

    /// <summary>
    /// Advisory cancellation flag. Subscribers may set this to true; publishers can
    /// check it after <see cref="EventBus{TAction}.Publish"/> returns to decide whether
    /// to proceed with the associated action.
    /// </summary>
    public bool Cancelled { get; set; }
}
