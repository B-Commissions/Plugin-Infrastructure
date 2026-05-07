using System;
using System.Threading.Tasks;
using BlueBeard.Core.Abstractions;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Users.Events;

namespace BlueBeard.OpenMod;

/// <summary>
/// Bridges OpenMod's <see cref="UnturnedUserConnectedEvent"/> /
/// <see cref="UnturnedUserDisconnectedEvent"/> into <see cref="IPlayerEvents"/>.
/// Subscribes via <see cref="IEventBus"/> on construction; call <see cref="Dispose"/> to
/// detach from the bus.
/// </summary>
public sealed class OpenModPlayerEvents : IPlayerEvents, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly IDisposable _connectedSub;
    private readonly IDisposable _disconnectedSub;

    public event Action<IPlayer> PlayerConnected;
    public event Action<IPlayer> PlayerDisconnected;

    public OpenModPlayerEvents(IEventBus eventBus, global::OpenMod.API.IOpenModComponent component)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _connectedSub = _eventBus.Subscribe<UnturnedUserConnectedEvent>(component, OnConnected);
        _disconnectedSub = _eventBus.Subscribe<UnturnedUserDisconnectedEvent>(component, OnDisconnected);
    }

    public void Dispose()
    {
        _connectedSub?.Dispose();
        _disconnectedSub?.Dispose();
        PlayerConnected = null;
        PlayerDisconnected = null;
    }

    private Task OnConnected(IServiceProvider _, object sender, UnturnedUserConnectedEvent evt)
    {
        PlayerConnected?.Invoke(OpenModPlayer.From(evt.User));
        return Task.CompletedTask;
    }

    private Task OnDisconnected(IServiceProvider _, object sender, UnturnedUserDisconnectedEvent evt)
    {
        PlayerDisconnected?.Invoke(OpenModPlayer.From(evt.User));
        return Task.CompletedTask;
    }
}
