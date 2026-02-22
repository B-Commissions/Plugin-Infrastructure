# Patterns

Patterns control the spatial layout of effect spawn points. Every pattern produces a sequence of `Vector3` offsets relative to the `EffectDefinition.Origin`. The emitter adds each offset to the origin to compute the final world position.

## The IEffectPattern Interface

```csharp
namespace BlueBeard.Effects.Patterns;

public interface IEffectPattern
{
    IEnumerable<Vector3> GetPoints();
}
```

`GetPoints()` returns an `IEnumerable<Vector3>` of offset vectors. Each vector is added to `EffectDefinition.Origin` at emit time. A pattern that yields `Vector3.zero` places the effect directly at the origin. A pattern that yields `new Vector3(5, 0, 0)` places it 5 units east of the origin.

---

## Built-in Patterns

### SinglePointPattern

The simplest pattern. Yields a single point at `Vector3.zero`, meaning the effect spawns exactly at the definition's origin.

```csharp
namespace BlueBeard.Effects.Patterns;

public class SinglePointPattern : IEffectPattern
{
    public IEnumerable<Vector3> GetPoints()
    {
        yield return Vector3.zero;
    }
}
```

**Usage:**

```csharp
var pattern = new SinglePointPattern();
```

Best for one-off effects at an exact location -- explosions, item pickups, hit markers.

---

### CirclePattern

Distributes points evenly around a horizontal circle (on the XZ plane) centered at the origin.

```csharp
public CirclePattern(float radius, int pointCount)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `radius` | `float` | Radius of the circle. Must be greater than 0. |
| `pointCount` | `int` | Number of points around the circumference. Must be greater than 0. |

**Validation:** Throws `ArgumentOutOfRangeException` if `radius` or `pointCount` is zero or negative.

**How it works:**

The step angle is `2 * PI / pointCount`. For each point `i`, the offset is:

```
x = cos(step * i) * radius
y = 0
z = sin(step * i) * radius
```

**Usage:**

```csharp
// 12 points in a circle with radius 10
var pattern = new CirclePattern(radius: 10f, pointCount: 12);
```

Best for zone borders, auras, ring-shaped visual indicators.

---

### SquarePattern

Distributes points along the four edges of a square on the XZ plane, centered at the origin.

```csharp
public SquarePattern(float size, int pointsPerSide)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `size` | `float` | Side length of the square. Must be greater than 0. |
| `pointsPerSide` | `int` | Number of points along each edge. Must be greater than 0. |

**Validation:** Throws `ArgumentOutOfRangeException` if `size` or `pointsPerSide` is zero or negative.

**How it works:**

The total number of points is `pointsPerSide * 4`. Points are generated in order: top edge (left to right), right edge (top to bottom), bottom edge (right to left), left edge (bottom to top). The step between points on each side is `size / pointsPerSide`.

**Usage:**

```csharp
// 20x20 square, 5 points per side (20 total)
var pattern = new SquarePattern(size: 20f, pointsPerSide: 5);
```

Best for rectangular zone outlines, building perimeters, area-of-effect borders.

---

### ScatterPattern

Places points at random positions within a radius, with optional minimum spacing between them. Unlike the other patterns, `ScatterPattern` pre-computes its offsets at construction time (the positions are not re-randomized on each emit cycle). It also snaps each position to the ground surface during construction using `SurfaceHelper.SnapPositionToSurface` and lifts them 0.1 units above the surface.

#### Constructor

```csharp
public ScatterPattern(Vector3 origin, IEnumerable<Vector3> absolutePositions)
```

This constructor takes an origin and a list of absolute world positions. Each position is snapped to the surface, lifted by 0.1 units on the Y axis, and then converted to an offset by subtracting the origin. You typically will not call this constructor directly -- use the static factory method instead.

#### Static Factory: Random

```csharp
public static ScatterPattern Random(Vector3 origin, int count, float radius, float minDistance = 0f)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `origin` | `Vector3` | World-space center of the scatter area. |
| `count` | `int` | Number of points to generate. Must be greater than 0. |
| `radius` | `float` | Maximum distance from origin (horizontal). Must be greater than 0. |
| `minDistance` | `float` | Minimum distance between any two points (optional, default 0). Must be non-negative. |

**Validation:** Throws `ArgumentOutOfRangeException` for invalid values.

**How it works:**

For each point, the method generates a random position within the circle defined by `radius` using `Random.insideUnitCircle`. If `minDistance` is set, it uses a Poisson-disc-style rejection approach: each candidate is checked against all previously placed points, and if it is too close it is discarded and a new candidate is tried (up to 30 attempts). If no valid position is found after 30 attempts, a random position is placed anyway to guarantee the requested `count`.

**Usage:**

```csharp
var origin = new Vector3(100f, 0f, 200f);

// 8 random points within 15 units, at least 3 units apart
var pattern = ScatterPattern.Random(origin, count: 8, radius: 15f, minDistance: 3f);
```

Best for debris fields, random environmental effects, battlefield scatter.

---

## Creating a Custom Pattern

To create your own pattern, implement `IEffectPattern` and yield offset vectors from `GetPoints()`.

### Example: Line Pattern

```csharp
using System.Collections.Generic;
using UnityEngine;
using BlueBeard.Effects.Patterns;

public class LinePattern : IEffectPattern
{
    private readonly Vector3 _direction;
    private readonly float _length;
    private readonly int _pointCount;

    public LinePattern(Vector3 direction, float length, int pointCount)
    {
        _direction = direction.normalized;
        _length = length;
        _pointCount = pointCount;
    }

    public IEnumerable<Vector3> GetPoints()
    {
        if (_pointCount <= 1)
        {
            yield return Vector3.zero;
            yield break;
        }

        var step = _length / (_pointCount - 1);
        var start = -_direction * (_length / 2f);

        for (var i = 0; i < _pointCount; i++)
        {
            yield return start + _direction * (step * i);
        }
    }
}
```

### Guidelines for Custom Patterns

1. **Return offsets, not absolute positions.** The emitter adds each offset to `EffectDefinition.Origin` automatically.
2. **Keep the Y component at 0** unless you specifically want vertical displacement. If `SnapToSurface` is enabled on the definition, the emitter will raycast positions to the ground anyway.
3. **Validate constructor parameters.** Throw `ArgumentOutOfRangeException` for invalid values to fail fast.
4. **Use `yield return`** for lazy evaluation when possible. The emitter iterates the points once per emit cycle, so there is no benefit to caching unless construction is expensive (as `ScatterPattern` does for its random generation).
