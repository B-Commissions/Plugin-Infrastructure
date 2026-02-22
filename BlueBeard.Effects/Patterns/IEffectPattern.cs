using System.Collections.Generic;
using UnityEngine;

namespace BlueBeard.Effects.Patterns;

public interface IEffectPattern
{
    IEnumerable<Vector3> GetPoints();
}
