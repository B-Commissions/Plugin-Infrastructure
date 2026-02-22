using System.Collections.Generic;
using SDG.NetTransport;

namespace BlueBeard.Effects.Audiences;

public interface IEffectAudience
{
    IEnumerable<ITransportConnection> GetRecipients();
}
