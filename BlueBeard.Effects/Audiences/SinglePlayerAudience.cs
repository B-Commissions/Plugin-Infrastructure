using System.Collections.Generic;
using SDG.NetTransport;
using SDG.Unturned;

namespace BlueBeard.Effects.Audiences;

public class SinglePlayerAudience(Player player) : IEffectAudience
{
    public IEnumerable<ITransportConnection> GetRecipients()
    {
        if (player != null && player.channel != null && player.channel.owner != null)
            yield return player.channel.owner.transportConnection;
    }
}
