using System;
using System.Collections.Generic;
using System.Linq;
using SDG.NetTransport;
using SDG.Unturned;

namespace BlueBeard.Effects.Audiences;

public class PlayerGroupAudience(Func<SteamPlayer, bool> predicate) : IEffectAudience
{
    private readonly Func<SteamPlayer, bool> _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

    public IEnumerable<ITransportConnection> GetRecipients() =>
        from client in Provider.clients where _predicate(client) select client.transportConnection;
}
