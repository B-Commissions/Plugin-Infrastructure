using BlueBeard.Effects.Patterns;
using UnityEngine;

namespace BlueBeard.Effects;

public class EffectDefinition
{
    public ushort EffectId { get; set; }
    public IEffectPattern Pattern { get; set; }
    public Vector3 Origin { get; set; }
    public float Interval { get; set; }
    public bool SnapToSurface { get; set; } = true;
    public bool OneShot { get; set; }

    /// <summary>
    /// Optional per-axis scale applied to every spawned effect. When null (the default)
    /// effects are emitted via the fast <see cref="SDG.Unturned.EffectManager.sendEffectReliable(ushort, SDG.NetTransport.ITransportConnection, UnityEngine.Vector3)"/>
    /// path at the asset's native scale. When set, emission uses
    /// <see cref="SDG.Unturned.TriggerEffectParameters"/> so the scale is forwarded to the client.
    /// </summary>
    public Vector3? Scale { get; set; }
}
