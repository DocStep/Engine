using System;
using System.Numerics;

namespace Engine.Graphics;


public static class Raycaster {

    public static Ray ScreenPointToRay (Vector2 mousePos, int screenWidth, int screenHeight, Matrix4x4 view, Matrix4x4 proj) {
        float x = (2f*mousePos.X) / screenWidth - 1f;
        float y = 1f - (2f*mousePos.Y) / screenHeight;

        Matrix4x4.Invert(view*proj, out var invVP);

        var nearPoint = Vector4.Transform(new Vector4(x, y, -1f, 1f), invVP);
        var farPoint = Vector4.Transform(new Vector4(x, y, 1f, 1f), invVP);

        nearPoint /= nearPoint.W;
        farPoint /= farPoint.W;

        var origin = new Vector3(nearPoint.X, nearPoint.Y, nearPoint.Z);
        var dir = Vector3.Normalize(new Vector3(farPoint.X, farPoint.Y, farPoint.Z) - origin);

        return new Ray(origin, dir);
    }

    public static bool RayAabb (Ray ray, AABB box, out float tHit) {
        float tMin = 0f;
        float tMax = float.MaxValue;
        tHit = 0f;

        for (int i = 0; i < 3; i++) {
            float origin = i == 0 ? ray.Origin.X : i == 1 ? ray.Origin.Y : ray.Origin.Z;
            float dir = i == 0 ? ray.Direction.X : i == 1 ? ray.Direction.Y : ray.Direction.Z;
            float min = i == 0 ? box.Min.X : i == 1 ? box.Min.Y : box.Min.Z;
            float max = i == 0 ? box.Max.X : i == 1 ? box.Max.Y : box.Max.Z;

            if (MathF.Abs(dir) < 1e-8f) {
                if (origin < min || origin > max)
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

            if (tMin > tMax)
                return false;
        }

        tHit = tMin;
        return true;
    }

    public static bool RayTriangle (Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float t) {
        const float epsilon = 1e-6f;
        t = 0f;

        var edge1 = v1 - v0;
        var edge2 = v2 - v0;
        var h = Vector3.Cross(ray.Direction, edge2);
        float a = Vector3.Dot(edge1, h);

        if (a > -epsilon && a < epsilon)
            return false; /// ray parallel to triangle

        float f = 1f/a;
        var s = ray.Origin - v0;
        float u = f*Vector3.Dot(s, h);

        if (u < 0f || u > 1f)
            return false;

        var q = Vector3.Cross(s, edge1);
        float v = f*Vector3.Dot(ray.Direction, q);

        if (v < 0f || u + v > 1f)
            return false;

        t = f*Vector3.Dot(edge2, q);

        return t > epsilon;
    }

    public static bool RaycastMesh (Ray ray, MeshData mesh, Matrix4x4 worldMatrix, out Vector3 worldHit, out float closestT, out Vector3 worldNormal) {
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

    public static bool RaycastScene (Scene scene, Ray ray, out MeshComponent? hitMesh, out Vector3 hitPoint, out Vector3 hitNormal) {
        hitMesh = null;
        hitPoint = default;
        hitNormal = default;
        float closestT = float.MaxValue;
        bool hitAny = false;

        foreach (var go in scene.Objects) {
            var meshComp = go.GetComponent<MeshComponent>();
            if (meshComp?.mesh is null) continue;

            var worldMatrix = go.Transform.GetWorldMatrix();
            var worldAabb = meshComp.mesh.LocalAABB.Transformed(worldMatrix);
            if (!RayAabb(ray, worldAabb, out float aabbT) || closestT < aabbT) continue; /// broadphase reject, can't be the closest hit

            if (RaycastMesh(ray, meshComp.mesh.Data, worldMatrix, out var localHitPoint, out float t, out var localNormal) && t < closestT) {
                closestT = t;
                hitMesh = meshComp;
                hitPoint = localHitPoint;
                hitNormal = localNormal;
                hitAny = true;
            }
        }

        return hitAny;
    }
}
