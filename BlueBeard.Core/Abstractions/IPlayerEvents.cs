using System;

namespace BlueBeard.Core.Abstractions;

/// <summary>
/// Framework-agnostic player lifecycle events. RocketMod adapter wraps
/// <c>U.Events.OnPlayerConnected/Disconnected</c>; OpenMod adapter subscribes to its
/// <c>UnturnedPlayerConnectedEvent</c> / <c>UnturnedPlayerDisconnectedEvent</c>.
/// </summary>
public interface IPlayerEvents
{
    event Action<IPlayer> PlayerConnected;
    event Action<IPlayer> PlayerDisconnected;
}
