using System.Collections.Generic;
using UnityEngine;

namespace BlueBeard.Effects.Patterns;

public class SinglePointPattern : IEffectPattern
{
    public IEnumerable<Vector3> GetPoints()
    {
        yield return Vector3.zero;
    }
}
