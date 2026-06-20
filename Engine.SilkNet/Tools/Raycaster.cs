using Silk.NET.Maths;

namespace Engine.SilkNet;


/// Simple analytic ray intersection tests used for mouse picking.
/// All tests return the ray parameter t (distance along the ray) on hit,
/// or null on miss. The hit point is `origin + direction*t`.
public static class Raycaster {
    /// Builds a world-space ray from a mouse position (pixels, origin top-left)
    /// through the camera, using the inverse view-projection matrix.
    public static (Vector3D<float> origin, Vector3D<float> direction) ScreenPointToRay (
        float mouseX, float mouseY, int viewportWidth, int viewportHeight,
        Matrix4X4<float> view, Matrix4X4<float> projection) {

        // Pixels -> normalized device coordinates [-1, 1], Y flipped (screen Y is top-down).
        float ndcX = (2f*mouseX) / viewportWidth - 1f;
        float ndcY = 1f - (2f*mouseY) / viewportHeight;

        Matrix4X4.Invert(view * projection, out Matrix4X4<float> inverseViewProjection);

        Vector4D<float> nearPoint4 = Vector4D.Transform(new Vector4D<float>(ndcX, ndcY, -1f, 1f), inverseViewProjection);
        Vector4D<float> farPoint4 = Vector4D.Transform(new Vector4D<float>(ndcX, ndcY, 1f, 1f), inverseViewProjection);

        Vector3D<float> nearPoint = new Vector3D<float>(nearPoint4.X, nearPoint4.Y, nearPoint4.Z) / nearPoint4.W;
        Vector3D<float> farPoint = new Vector3D<float>(farPoint4.X, farPoint4.Y, farPoint4.Z) / farPoint4.W;

        Vector3D<float> direction = Vector3D.Normalize(farPoint - nearPoint);
        return (nearPoint, direction);
    }

    public static float? IntersectSphere (Vector3D<float> origin, Vector3D<float> direction, Vector3D<float> center, float radius) {
        Vector3D<float> oc = origin - center;
        float b = Vector3D.Dot(oc, direction);
        float c = Vector3D.Dot(oc, oc) - radius*radius;
        float discriminant = b*b - c;
        if (discriminant < 0f) return null;

        float sqrtDiscriminant = MathF.Sqrt(discriminant);
        float t0 = -b - sqrtDiscriminant;
        float t1 = -b + sqrtDiscriminant;

        if (t0 >= 0f) return t0;
        if (t1 >= 0f) return t1;
        return null;
    }

    /// Axis-aligned box test, given the box's local min/max corners and a world model matrix
    /// (translation + uniform/non-uniform scale; no rotation support needed here).
    public static float? IntersectAabb (Vector3D<float> origin, Vector3D<float> direction, Vector3D<float> worldMin, Vector3D<float> worldMax) {
        float tMin = float.NegativeInfinity;
        float tMax = float.PositiveInfinity;

        for (int axis = 0; axis < 3; axis++) {
            float originAxis = GetAxis(origin, axis);
            float dirAxis = GetAxis(direction, axis);
            float minAxis = GetAxis(worldMin, axis);
            float maxAxis = GetAxis(worldMax, axis);

            if (MathF.Abs(dirAxis) < 1e-8f) {
                if (originAxis < minAxis || originAxis > maxAxis) return null;
                continue;
            }

            float t1 = (minAxis - originAxis) / dirAxis;
            float t2 = (maxAxis - originAxis) / dirAxis;
            if (t1 > t2) (t1, t2) = (t2, t1);

            tMin = MathF.Max(tMin, t1);
            tMax = MathF.Min(tMax, t2);
            if (tMin > tMax) return null;
        }

        if (tMax < 0f) return null;
        return tMin >= 0f ? tMin : tMax;
    }

    /// Infinite plane test (plane defined by a point on it and a normal).
    public static float? IntersectPlane (Vector3D<float> origin, Vector3D<float> direction, Vector3D<float> planePoint, Vector3D<float> planeNormal) {
        float denominator = Vector3D.Dot(direction, planeNormal);
        if (MathF.Abs(denominator) < 1e-8f) return null; // ray parallel to plane

        float t = Vector3D.Dot(planePoint - origin, planeNormal) / denominator;
        return t >= 0f ? t : null;
    }

    private static float GetAxis (Vector3D<float> v, int axis) => axis switch {
        0 => v.X,
        1 => v.Y,
        _ => v.Z,
    };
}