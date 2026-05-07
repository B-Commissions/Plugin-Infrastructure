using System;
using BlueBeard.Core.Abstractions;
using Rocket.Unturned;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;

namespace BlueBeard.RocketMod;

/// <summary>
/// Wires <see cref="U.Events"/> player connect/disconnect signals into the
/// framework-agnostic <see cref="IPlayerEvents"/> contract. Subscriptions are added on
/// construction; call <see cref="Dispose"/> from the bootstrap shutdown path to detach.
/// </summary>
public sealed class RocketPlayerEvents : IPlayerEvents, IDisposable
{
    public event Action<IPlayer> PlayerConnected;
    public event Action<IPlayer> PlayerDisconnected;

    public RocketPlayerEvents()
    {
        U.Events.OnPlayerConnected += OnConnected;
        U.Events.OnPlayerDisconnected += OnDisconnected;
    }

    public void Dispose()
    {
        U.Events.OnPlayerConnected -= OnConnected;
        U.Events.OnPlayerDisconnected -= OnDisconnected;
        PlayerConnected = null;
        PlayerDisconnected = null;
    }

    private void OnConnected(UnturnedPlayer player)
        => PlayerConnected?.Invoke(RocketPlayer.From(player));

    private void OnDisconnected(UnturnedPlayer player)
        => PlayerDisconnected?.Invoke(RocketPlayer.From(player));
}
