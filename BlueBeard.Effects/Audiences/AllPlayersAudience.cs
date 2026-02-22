using System.Collections.Generic;
using System.Linq;
using SDG.NetTransport;
using SDG.Unturned;

namespace BlueBeard.Effects.Audiences;

public class AllPlayersAudience : IEffectAudience
{
    public IEnumerable<ITransportConnection> GetRecipients() =>
        Provider.clients.Select(client => client.transportConnection);
}
