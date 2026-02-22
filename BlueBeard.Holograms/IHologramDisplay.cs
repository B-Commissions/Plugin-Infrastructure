using System.Collections.Generic;
using SDG.NetTransport;

namespace BlueBeard.Holograms;

public interface IHologramDisplay
{
    void Show(ITransportConnection connection, short key,
        HologramDefinition definition, Dictionary<string, string> metadata);

    void Hide(ITransportConnection connection, short key, HologramDefinition definition);
}
