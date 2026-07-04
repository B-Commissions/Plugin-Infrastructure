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

    // Set by the owning bus; kept as a delegate so the public IEventBus interface stays
    // untouched (adding interface members breaks external implementors).
    internal Action<Subscription> Unsubscriber;

    internal Subscription(long id)
    {
        Id = id;
    }

    /// <summary>Unsubscribe from the owning bus. Safe to call multiple times.</summary>
    public void Dispose() => Unsubscriber?.Invoke(this);
}
