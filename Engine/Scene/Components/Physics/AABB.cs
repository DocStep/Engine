using System.Numerics;

namespace Engine;


public struct AABB {
    public Vector3 min;
    public Vector3 max;

    public bool Intersects (AABB other) {
        return min.X <= other.max.X && max.X >= other.min.X
            && min.Y <= other.max.Y && max.Y >= other.min.Y
            && min.Z <= other.max.Z && max.Z >= other.min.Z;
    }
}
