using System;

namespace BlueBeard.Events;

/// <summary>
/// Opaque handle returned by <see cref="EventBus{TAction}.Subscribe"/>. Pass this to
/// <see cref="EventBus{TAction}.Unsubscribe"/> to remove the subscription — or just
/// dispose it (composes with <c>using</c> and aggregate disposal).
/// </summary>
public sealed class Subscription : IDisposable
{
    internal long Id { get; }

    private readonly IEventBus _owner;

    internal Subscription(long id)
    {
        Id = id;
    }

    internal Subscription(long id, IEventBus owner)
    {
        Id = id;
        _owner = owner;
    }

    /// <summary>Unsubscribe from the owning bus. Safe to call multiple times.</summary>
    public void Dispose() => _owner?.Unsubscribe(this);
}
