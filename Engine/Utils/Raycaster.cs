using System.Numerics;

namespace Engine;

public static class Raycaster {
    public static (Vector3 origin, Vector3 direction) ScreenPointToRay (
        float mouseX, float mouseY, int viewportWidth, int viewportHeight,
        Matrix4x4 view, Matrix4x4 projection) {

        /// Pixels -> normalized device coordinates [-1, 1], Y flipped (screen Y is top-down).
        float ndcX = (2f*mouseX)/viewportWidth - 1f;
        float ndcY = 1f - (2f*mouseY)/viewportHeight;

        Matrix4x4.Invert(view*projection, out Matrix4x4 inverseViewProjection);

        Vector4 nearPoint4 = Vector4.Transform(new Vector4(ndcX, ndcY, -1f, 1f), inverseViewProjection);
        Vector4 farPoint4 = Vector4.Transform(new Vector4(ndcX, ndcY, 1f, 1f), inverseViewProjection);

        Vector3 nearPoint = new Vector3(nearPoint4.X, nearPoint4.Y, nearPoint4.Z)/nearPoint4.W;
        Vector3 farPoint = new Vector3(farPoint4.X, farPoint4.Y, farPoint4.Z)/farPoint4.W;

        Vector3 direction = Vector3.Normalize(farPoint - nearPoint);
        return (nearPoint, direction);
    }

    public static float? IntersectSphere (Vector3 origin, Vector3 direction, Vector3 center, float radius) {
        Vector3 oc = origin - center;
        float b = Vector3.Dot(oc, direction);
        float c = Vector3.Dot(oc, oc) - radius*radius;
        float discriminant = b*b - c;
        if (discriminant < 0f) return null;

        float sqrtDiscriminant = MathF.Sqrt(discriminant);
        float t0 = -b - sqrtDiscriminant;
        float t1 = -b + sqrtDiscriminant;

        if (t0 >= 0f) return t0;
        if (t1 >= 0f) return t1;
        return null;
    }

    internal static float? IntersectAABB (Vector3 origin, Vector3 dir, Vector3 min, Vector3 max) {
        float tMin = float.NegativeInfinity;
        float tMax = float.PositiveInfinity;

        Span<float> o = stackalloc float[] { origin.X, origin.Y, origin.Z };
        Span<float> d = stackalloc float[] { dir.X, dir.Y, dir.Z };
        Span<float> mn = stackalloc float[] { min.X, min.Y, min.Z };
        Span<float> mx = stackalloc float[] { max.X, max.Y, max.Z };

        for (int i = 0; i < 3; i++) {
            if (MathF.Abs(d[i]) < 1e-8f) {
                if (o[i] < mn[i] || o[i] > mx[i]) return null;
            } else {
                float t1 = (mn[i] - o[i])/d[i];
                float t2 = (mx[i] - o[i])/d[i];
                if (t1 > t2) (t1, t2) = (t2, t1);
                tMin = MathF.Max(tMin, t1);
                tMax = MathF.Min(tMax, t2);
                if (tMin > tMax) return null;
            }
        }

        if (tMin > 0f) return tMin;
        if (tMax > 0f) return tMax;
        return null;
    }

    /// Möller–Trumbore
    internal static float? IntersectTriangle (
        Vector3 origin, Vector3 dir,
        Vector3 v0, Vector3 v1, Vector3 v2) {
        const float EPSILON = 1e-8f;

        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;
        Vector3 h = Vector3.Cross(dir, edge2);
        float det = Vector3.Dot(edge1, h);

        if (MathF.Abs(det) < EPSILON) return null; /// parallel

        float invDet = 1f/det;
        Vector3 s = origin - v0;
        float u = invDet*Vector3.Dot(s, h);
        if (u < 0f || u > 1f) return null;

        Vector3 q = Vector3.Cross(s, edge1);
        float v = invDet*Vector3.Dot(dir, q);
        if (v < 0f || u + v > 1f) return null;

        float t = invDet*Vector3.Dot(edge2, q);
        return t > EPSILON ? t : null;
    }

    public static float? IntersectPlane (Vector3 origin, Vector3 direction, Vector3 planePoint, Vector3 planeNormal) {
        float denominator = Vector3.Dot(direction, planeNormal);
        if (MathF.Abs(denominator) < 1e-8f) return null; /// ray parallel to plane

        float t = Vector3.Dot(planePoint - origin, planeNormal)/denominator;
        return t >= 0f ? t : null;
    }

    private static float GetAxis (Vector3 v, int axis) => axis switch {
        0 => v.X,
        1 => v.Y,
        _ => v.Z,
    };
}
