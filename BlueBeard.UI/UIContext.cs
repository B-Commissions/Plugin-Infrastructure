using Rocket.Unturned.Player;
using SDG.NetTransport;

namespace BlueBeard.UI;

public class UIContext
{
    public UnturnedPlayer Player { get; }
    public ITransportConnection Connection { get; }
    public short EffectKey { get; }
    public UIPlayerComponent Component { get; }

    public UIContext(UnturnedPlayer player, ITransportConnection connection, short effectKey, UIPlayerComponent component)
    {
        Player = player;
        Connection = connection;
        EffectKey = effectKey;
        Component = component;
    }
}
