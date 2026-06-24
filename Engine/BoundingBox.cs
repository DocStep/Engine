using System.Numerics;

namespace Engine;


internal readonly struct BoundingBox {
    internal readonly Vector3 Min;
    internal readonly Vector3 Max;
    internal BoundingBox (Vector3 min, Vector3 max) { Min = min; Max = max; }
}
