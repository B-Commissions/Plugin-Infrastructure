using Rocket.Unturned.Player;
using SDG.NetTransport;

namespace BlueBeard.UI;

public class UIContext(UnturnedPlayer player, ITransportConnection connection, short effectKey, UIPlayerComponent component)
{
    public UnturnedPlayer Player { get; } = player;
    public ITransportConnection Connection { get; } = connection;
    public short EffectKey { get; } = effectKey;
    public UIPlayerComponent Component { get; } = component;
}
