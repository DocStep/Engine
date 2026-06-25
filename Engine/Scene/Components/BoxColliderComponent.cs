using System;
using System.Numerics;
using System.Collections.Generic;

namespace Engine;


public class BoxColliderComponent : ColliderComponent {

    public Vector3 position = Vector3.Zero;
    public Vector3 rotation = Vector3.Zero;
    public Vector3 scale = Vector3.One;


    public override void Update () {
        if (!drawGizmos) return;

        Graphics.RenderInfo renderInfo = new Graphics.RenderInfo() {
            pos = position + owner.Transform.Position,
            rot = rotation + owner.Transform.Rotation,
            scale = scale*owner.Transform.Scale,

            mesh = Graphics.Renderer.Instance._mesh_GizmoCube,
            shader = Graphics.Renderer.Instance._sh_Unlit,
            material = Graphics.Renderer.Instance._mat_GizmosG,
            primitiveType = Silk.NET.OpenGL.PrimitiveType.Lines,
        };
        Graphics.Renderer.Instance.AddRenderInfo(renderInfo);
    }

    /// Get the oriented bounding box center and half-extents
    private (Vector3 center, Vector3 halfExtents, Quaternion rotation) GetOBB () {
        Vector3 center = position + owner.Transform.Position;
        Vector3 halfExtents = 0.5f * scale * owner.Transform.Scale;
        Quaternion rot = QuaternionFromEuler(this.rotation + owner.Transform.Rotation);
        return (center, halfExtents, rot);
    }

    /// Convert Euler angles (in radians) to Quaternion
    private static Quaternion QuaternionFromEuler (Vector3 euler) {
        Vector3 c = new Vector3(MathF.Cos(euler.X * 0.5f), MathF.Cos(euler.Y * 0.5f), MathF.Cos(euler.Z * 0.5f));
        Vector3 s = new Vector3(MathF.Sin(euler.X * 0.5f), MathF.Sin(euler.Y * 0.5f), MathF.Sin(euler.Z * 0.5f));

        return new Quaternion(
            s.X * c.Y * c.Z - c.X * s.Y * s.Z,
            c.X * s.Y * c.Z + s.X * c.Y * s.Z,
            c.X * c.Y * s.Z - s.X * s.Y * c.Z,
            c.X * c.Y * c.Z + s.X * s.Y * s.Z
        );
    }

    /// Get the world-space bounds of the rotated box
    public override Bounds GetWorldBounds () {
        var (center, halfExtents, quat) = GetOBB();

        // Get the 8 corners of the local box
        Vector3[] corners = new[] {
            center + new Vector3(-halfExtents.X, -halfExtents.Y, -halfExtents.Z),
            center + new Vector3(halfExtents.X, -halfExtents.Y, -halfExtents.Z),
            center + new Vector3(halfExtents.X, halfExtents.Y, -halfExtents.Z),
            center + new Vector3(-halfExtents.X, halfExtents.Y, -halfExtents.Z),
            center + new Vector3(-halfExtents.X, -halfExtents.Y, halfExtents.Z),
            center + new Vector3(halfExtents.X, -halfExtents.Y, halfExtents.Z),
            center + new Vector3(halfExtents.X, halfExtents.Y, halfExtents.Z),
            center + new Vector3(-halfExtents.X, halfExtents.Y, halfExtents.Z),
        };

        // Rotate corners around center
        Vector3 min = new Vector3(float.MaxValue);
        Vector3 max = new Vector3(float.MinValue);

        foreach (Vector3 corner in corners) {
            Vector3 offset = corner - center;
            Vector3 rotated = RotateVector(offset, quat);
            Vector3 worldCorner = center + rotated;

            min = Vector3.Min(min, worldCorner);
            max = Vector3.Max(max, worldCorner);
        }

        return new Bounds(min, max);
    }

    /// Rotate a vector by a quaternion
    private static Vector3 RotateVector (Vector3 v, Quaternion q) {
        float xx = q.X * q.X;
        float yy = q.Y * q.Y;
        float zz = q.Z * q.Z;
        float xy = q.X * q.Y;
        float zw = q.Z * q.W;
        float zx = q.Z * q.X;
        float yw = q.Y * q.W;
        float yz = q.Y * q.Z;
        float xw = q.X * q.W;

        return new Vector3(
            (1 - 2 * (yy + zz)) * v.X + (2 * (xy - zw)) * v.Y + (2 * (zx + yw)) * v.Z,
            (2 * (xy + zw)) * v.X + (1 - 2 * (xx + zz)) * v.Y + (2 * (yz - xw)) * v.Z,
            (2 * (zx - yw)) * v.X + (2 * (yz + xw)) * v.Y + (1 - 2 * (xx + yy)) * v.Z
        );
    }

    /// Simple AABB collision detection with proper normal calculation
    public override bool Overlaps (ColliderComponent other, out Contact contact) {
        contact = default;

        if (other is BoxColliderComponent otherBox) {
            return OverlapsBoxAABB(otherBox, out contact);
        }

        return false;
    }

    private bool OverlapsBoxAABB (BoxColliderComponent other, out Contact contact) {
        contact = default;

        Bounds a = GetWorldBounds();
        Bounds b = other.GetWorldBounds();

        // Check if AABB overlap exists
        Vector3 overlap = new Vector3(
            MathF.Min(a.Max.X, b.Max.X) - MathF.Max(a.Min.X, b.Min.X),
            MathF.Min(a.Max.Y, b.Max.Y) - MathF.Max(a.Min.Y, b.Min.Y),
            MathF.Min(a.Max.Z, b.Max.Z) - MathF.Max(a.Min.Z, b.Min.Z)
        );

        // No collision if any axis shows separation
        if (overlap.X <= 0f || overlap.Y <= 0f || overlap.Z <= 0f) {
            return false;
        }

        // Find the axis with minimum penetration (most likely separation direction)
        Vector3 normal = Vector3.Zero;
        float minPenetration = float.MaxValue;

        if (overlap.X < minPenetration) {
            minPenetration = overlap.X;
            normal = new Vector3(1f, 0f, 0f);
        }
        if (overlap.Y < minPenetration) {
            minPenetration = overlap.Y;
            normal = new Vector3(0f, 1f, 0f);
        }
        if (overlap.Z < minPenetration) {
            minPenetration = overlap.Z;
            normal = new Vector3(0f, 0f, 1f);
        }

        // Ensure normal points from B to A (away from B)
        Vector3 delta = a.Center - b.Center;
        if (Vector3.Dot(delta, normal) < 0f) {
            normal = -normal;
        }

        contact = new Contact {
            normal = normal,
            penetration = minPenetration
        };

        return true;
    }

}
