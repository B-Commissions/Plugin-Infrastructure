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
}
