using Silk.NET.Maths;

namespace Engine;


public static class Raycaster {
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

    internal static float? IntersectAABB (Vector3D<float> origin, Vector3D<float> dir, Vector3D<float> min, Vector3D<float> max) {
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
                float t1 = (mn[i] - o[i]) / d[i];
                float t2 = (mx[i] - o[i]) / d[i];
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

    // Möller–Trumbore
    internal static float? IntersectTriangle (
        Vector3D<float> origin, Vector3D<float> dir,
        Vector3D<float> v0, Vector3D<float> v1, Vector3D<float> v2) {
        const float EPSILON = 1e-8f;

        Vector3D<float> edge1 = v1 - v0;
        Vector3D<float> edge2 = v2 - v0;
        Vector3D<float> h = Vector3D.Cross(dir, edge2);
        float det = Vector3D.Dot(edge1, h);

        if (MathF.Abs(det) < EPSILON) return null; // parallel

        float invDet = 1f / det;
        Vector3D<float> s = origin - v0;
        float u = invDet * Vector3D.Dot(s, h);
        if (u < 0f || u > 1f) return null;

        Vector3D<float> q = Vector3D.Cross(s, edge1);
        float v = invDet * Vector3D.Dot(dir, q);
        if (v < 0f || u + v > 1f) return null;

        float t = invDet * Vector3D.Dot(edge2, q);
        return t > EPSILON ? t : null;
    }

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
