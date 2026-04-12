using System.Collections.Generic;
using SDG.NetTransport;
using SDG.Unturned;
using UnityEngine;

namespace BlueBeard.Effects.Audiences;

/// <summary>
/// Sends the effect only to players within <paramref name="radius"/> of a fixed
/// <paramref name="origin"/>. Evaluated each emit cycle, so players moving in/out of
/// range are naturally included/excluded.
/// </summary>
public class ProximityAudience(Vector3 origin, float radius) : IEffectAudience
{
    private readonly float _sqrRadius = radius * radius;

    public IEnumerable<ITransportConnection> GetRecipients()
    {
        foreach (var client in Provider.clients)
        {
            if (client?.player == null) continue;
            var sqrDist = (client.player.transform.position - origin).sqrMagnitude;
            if (sqrDist <= _sqrRadius)
                yield return client.transportConnection;
        }
    }
}
