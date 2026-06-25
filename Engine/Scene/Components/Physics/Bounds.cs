using System.Numerics;

namespace Engine;


public struct Bounds {

    public Vector3 Min;
    public Vector3 Max;

    public Vector3 Center => 0.5f*(Min + Max);
    public Vector3 Size => Max - Min;
    public Vector3 HalfExtents => 0.5f*(Max - Min);

    public Bounds (Vector3 min, Vector3 max) {
        Min = min;
        Max = max;
    }

    public bool Intersects (Bounds other) {
        return Min.X <= other.Max.X && Max.X >= other.Min.X
            && Min.Y <= other.Max.Y && Max.Y >= other.Min.Y
            && Min.Z <= other.Max.Z && Max.Z >= other.Min.Z;
    }

    public bool Contains (Vector3 point) {
        return point.X >= Min.X && point.X <= Max.X
            && point.Y >= Min.Y && point.Y <= Max.Y
            && point.Z >= Min.Z && point.Z <= Max.Z;
    }

    public static Bounds Encapsulate (Bounds a, Bounds b) {
        return new Bounds(
            Vector3.Min(a.Min, b.Min),
            Vector3.Max(a.Max, b.Max)
        );
    }

}
