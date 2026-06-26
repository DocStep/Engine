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
    private  (Vector3 center, Vector3 halfExtents, Quaternion rotation) GetOBB () {
        Vector3 center = position + owner.Transform.Position;
        Vector3 halfExtents = 0.5f * scale * owner.Transform.Scale;
        Quaternion rot = PhysicsComponent.QuaternionFromEuler(this.rotation + owner.Transform.Rotation);
        return (center, halfExtents, rot);
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
            return OverlapsBoxOBB(otherBox, out contact);
        }

        return false;
    }

    private bool OverlapsBoxOBB (BoxColliderComponent other, out Contact contact) {
        contact = default;

        var (centerA, halfA, rotA) = GetOBB();
        var (centerB, halfB, rotB) = other.GetOBB();

        // Get axes for A and B (local X, Y, Z rotated into world space)
        Vector3[] axesA = {
            RotateVector(Vector3.UnitX, rotA),
            RotateVector(Vector3.UnitY, rotA),
            RotateVector(Vector3.UnitZ, rotA),
        };
            Vector3[] axesB = {
            RotateVector(Vector3.UnitX, rotB),
            RotateVector(Vector3.UnitY, rotB),
            RotateVector(Vector3.UnitZ, rotB),
        };

        Vector3 translation = centerB - centerA;

        float minPen = float.MaxValue;
        Vector3 minAxis = Vector3.Zero;

        // Test all 15 axes: 3 from A, 3 from B, 9 cross products
        Span<Vector3> testAxes = stackalloc Vector3[15];
        testAxes[0] = axesA[0]; testAxes[1] = axesA[1]; testAxes[2] = axesA[2];
        testAxes[3] = axesB[0]; testAxes[4] = axesB[1]; testAxes[5] = axesB[2];
        int idx = 6;
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                testAxes[idx++] = Vector3.Cross(axesA[i], axesB[j]);

        for (int i = 0; i < 15; i++) {
            Vector3 axis = testAxes[i];
            if (axis.LengthSquared() < 1e-6f) continue; // skip degenerate cross products
            axis = Vector3.Normalize(axis);

            float projA = MathF.Abs(Vector3.Dot(axesA[0] * halfA.X, axis))
                        + MathF.Abs(Vector3.Dot(axesA[1] * halfA.Y, axis))
                        + MathF.Abs(Vector3.Dot(axesA[2] * halfA.Z, axis));
            float projB = MathF.Abs(Vector3.Dot(axesB[0] * halfB.X, axis))
                        + MathF.Abs(Vector3.Dot(axesB[1] * halfB.Y, axis))
                        + MathF.Abs(Vector3.Dot(axesB[2] * halfB.Z, axis));

            float dist = MathF.Abs(Vector3.Dot(translation, axis));
            float pen = projA + projB - dist;

            if (pen <= 0f) return false; // separating axis found

            if (pen < minPen) {
                minPen = pen;
                minAxis = axis;
            }
        }

        // Ensure normal points from B to A
        if (Vector3.Dot(translation, minAxis) > 0f)
            minAxis = -minAxis;

        Vector3 deepest = centerA;
        deepest += Vector3.Dot(axesA[0], -minAxis) > 0 ? axesA[0] * halfA.X : -axesA[0] * halfA.X;
        deepest += Vector3.Dot(axesA[1], -minAxis) > 0 ? axesA[1] * halfA.Y : -axesA[1] * halfA.Y;
        deepest += Vector3.Dot(axesA[2], -minAxis) > 0 ? axesA[2] * halfA.Z : -axesA[2] * halfA.Z;

        contact = new Contact {
            normal = minAxis,
            penetration = minPen,
            point = deepest
        };

        return true;
    }

}
