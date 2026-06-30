using System;
using System.Numerics;

namespace Engine;


public static class Raycast {

    public static Ray ScreenPointToRay (float _x, float _y, int screenWidth, int screenHeight, Matrix4x4 view, Matrix4x4 proj) {
        float x = (2f*_x) / screenWidth - 1f;
        float y = 1f - (2f*_y) / screenHeight;

        Matrix4x4.Invert(view*proj, out var invVP);

        Vector4 nearPoint = Vector4.Transform(new Vector4(x, y, -1f, 1f), invVP);
        Vector4 farPoint = Vector4.Transform(new Vector4(x, y, 1f, 1f), invVP);
        nearPoint /= nearPoint.W;
        farPoint /= farPoint.W;

        var origin = new Vector3(nearPoint.X, nearPoint.Y, nearPoint.Z);
        var dir = Vector3.Normalize(new Vector3(farPoint.X, farPoint.Y, farPoint.Z) - origin);

        return new Ray(origin, dir);
    }

    public static bool RayAABB (Ray ray, AABB box, out float tHit) {
        float tMin = 0f;
        float tMax = float.MaxValue;
        tHit = 0f;

        for (int i = 0; i < 3; i++) {
            float origin = i == 0 ? ray.Origin.X : i == 1 ? ray.Origin.Y : ray.Origin.Z;
            float dir = i == 0 ? ray.Direction.X : i == 1 ? ray.Direction.Y : ray.Direction.Z;
            float min = i == 0 ? box.Min.X : i == 1 ? box.Min.Y : box.Min.Z;
            float max = i == 0 ? box.Max.X : i == 1 ? box.Max.Y : box.Max.Z;

            if (MathF.Abs(dir) < 1e-8f) {
                if (origin < min || max < origin)
                    return false; /// parallel and outside slab
                continue;
            }

            float invD = 1f/dir;
            float t1 = (min - origin)*invD;
            float t2 = (max - origin)*invD;

            if (t1 > t2)
                (t1, t2) = (t2, t1);

            tMin = MathF.Max(tMin, t1);
            tMax = MathF.Min(tMax, t2);

            if (tMax < tMin)
                return false;
        }

        tHit = tMin;
        return true;
    }

    public static Vector3? IntersectPlane (Ray ray, Vector3 planePoint, Vector3 planeNormal) {
        float denominator = Vector3.Dot(ray.Direction, planeNormal);
        if (MathF.Abs(denominator) < 1e-8f) return null; /// ray parallel to plane

        float t = Vector3.Dot(planePoint - ray.Origin, planeNormal)/denominator;
        if (0 <= t) return ray.Origin + t*ray.Direction;
        return null;
    }
    public static float? IntersectPlane (Vector3 origin, Vector3 direction, Vector3 planePoint, Vector3 planeNormal) {
        float denominator = Vector3.Dot(direction, planeNormal);
        if (MathF.Abs(denominator) < 1e-8f) return null; /// ray parallel to plane

        float t = Vector3.Dot(planePoint - origin, planeNormal)/denominator;
        return 0 <= t ? t : null;
    }
    public static Vector3? IntersectPlaneEuler (Ray ray, Vector3 planePoint, Vector3 planeEulerDeg) {
        Matrix4x4 rot = Matrix4x4.RotationEuler(planeEulerDeg);
        Vector3 planeNormal = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitZ, rot));

        float denominator = Vector3.Dot(ray.Direction, planeNormal);
        if (MathF.Abs(denominator) < 1e-8f) return null; /// ray parallel to plane

        float t = Vector3.Dot(planePoint - ray.Origin, planeNormal)/denominator;
        if (t < 0) return null;

        return ray.Origin + ray.Direction*t;
    }

    public static bool RayTriangle (Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float t) {
        const float epsilon = 1e-6f;
        t = 0f;

        var edge1 = v1 - v0;
        var edge2 = v2 - v0;
        var h = Vector3.Cross(ray.Direction, edge2);
        float a = Vector3.Dot(edge1, h);

        if (-epsilon < a && a < epsilon)
            return false; /// ray parallel to triangle

        float f = 1f/a;
        var s = ray.Origin - v0;
        float u = f*Vector3.Dot(s, h);

        if (u < 0f || 1 < u) return false;

        var q = Vector3.Cross(s, edge1);
        float v = f*Vector3.Dot(ray.Direction, q);

        if (v < 0 || 1 < u + v) return false;

        t = f*Vector3.Dot(edge2, q);

        return epsilon < t;
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
        if (u < 0f || 1 < u) return null;

        Vector3 q = Vector3.Cross(s, edge1);
        float v = invDet*Vector3.Dot(dir, q);
        if (0 < v || 1 < u + v) return null;

        float t = invDet*Vector3.Dot(edge2, q);
        return EPSILON < t ? t : null;
    }

    public static bool RaycastMesh (Ray ray, Graphics.MeshData mesh, Matrix4x4 worldMatrix, out Vector3 worldHit, out float closestT, out Vector3 worldNormal) {
        closestT = float.MaxValue;
        worldHit = default;
        worldNormal = default;
        bool hit = false;

        for (int i = 0; i < mesh.Indices.Length; i += 3) {
            var v0 = Vector3.Transform(mesh.Vertices[mesh.Indices[i]].Position, worldMatrix);
            var v1 = Vector3.Transform(mesh.Vertices[mesh.Indices[i + 1]].Position, worldMatrix);
            var v2 = Vector3.Transform(mesh.Vertices[mesh.Indices[i + 2]].Position, worldMatrix);

            if (RayTriangle(ray, v0, v1, v2, out float t) && t < closestT) {
                closestT = t;
                worldHit = ray.Origin + ray.Direction*t;
                worldNormal = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v0));
                hit = true;
            }
        }

        return hit;
    }

    public static bool RaycastScene (Scene scene, Ray ray, out Graphics.MeshComponent? hitMesh, out Vector3 hitPoint, out Vector3 hitNormal) {
        hitMesh = null;
        hitPoint = default;
        hitNormal = default;
        float closestT = float.MaxValue;
        bool hitAny = false;

        foreach (var go in scene.Objects) {
            Graphics.MeshComponent? meshComponent = go.GetComponent<Graphics.MeshComponent>();
            if (meshComponent is null) continue;
            if (meshComponent.mesh is null) continue;
            if (meshComponent.mesh.Data.PrimitiveType != Silk.NET.OpenGL.PrimitiveType.Triangles) continue;

            Matrix4x4 worldMatrix = go.Transform.GetWorldMatrix();
            AABB worldAabb = meshComponent.mesh.LocalAABB.Transformed(worldMatrix);
            if (!RayAABB(ray, worldAabb, out float aabbT) || closestT < aabbT) continue; /// broadphase reject, can't be the closest hit

            if (RaycastMesh(ray, meshComponent.mesh.Data, worldMatrix, out var localHitPoint, out float t, out var localNormal) && t < closestT) {
                closestT = t;
                hitMesh = meshComponent;
                hitPoint = localHitPoint;
                hitNormal = localNormal;
                hitAny = true;
            }
        }

        return hitAny;
    }


    public static float? IntersectSphere (Vector3 origin, Vector3 direction, Vector3 center, float radius) {
        Vector3 oc = origin - center;
        float b = Vector3.Dot(oc, direction);
        float c = Vector3.Dot(oc, oc) - radius*radius;
        float discriminant = b*b - c;
        if (discriminant < 0) return null;

        float sqrtDiscriminant = MathF.Sqrt(discriminant);
        float t0 = -b - sqrtDiscriminant;
        float t1 = -b + sqrtDiscriminant;

        if (0 <= t0) return t0;
        if (0 <= t1) return t1;
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
                if (o[i] < mn[i] || mx[i] < o[i]) return null;
            } else {
                float t1 = (mn[i] - o[i])/d[i];
                float t2 = (mx[i] - o[i])/d[i];
                if (t2 < t1) (t1, t2) = (t2, t1);
                tMin = MathF.Max(tMin, t1);
                tMax = MathF.Min(tMax, t2);
                if (tMax < tMin) return null;
            }
        }

        if (0 < tMin) return tMin;
        if (0 < tMax) return tMax;
        return null;
    }


    private static float GetAxis (Vector3 v, int axis) => axis switch {
        0 => v.X,
        1 => v.Y,
        _ => v.Z,
    };

}
