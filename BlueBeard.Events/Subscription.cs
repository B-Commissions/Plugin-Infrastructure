namespace BlueBeard.Events;

/// <summary>
/// Opaque handle returned by <see cref="EventBus{TAction}.Subscribe"/>. Pass this to
/// <see cref="EventBus{TAction}.Unsubscribe"/> to remove the subscription.
/// </summary>
public sealed class Subscription
{
    internal long Id { get; }

    internal Subscription(long id)
    {
        Id = id;
    }
}
