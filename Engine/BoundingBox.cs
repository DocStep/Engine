using Silk.NET.Maths;

namespace Engine;


internal readonly struct BoundingBox {
    internal readonly Vector3D<float> Min;
    internal readonly Vector3D<float> Max;
    internal BoundingBox (Vector3D<float> min, Vector3D<float> max) { Min = min; Max = max; }
}
