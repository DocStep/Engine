using System;
using System.Numerics;

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

    /// world-space half-extents, accounting for owner transform and local offset
    public override Bounds GetWorldBounds () {
        Vector3 center = position + owner.Transform.Position;
        Vector3 halfExtents = 0.5f*scale*owner.Transform.Scale;
        return new Bounds(center - halfExtents, center + halfExtents);
    }

    /// axis-aligned overlap test; rotation ignored for now (AABB only)
    public override bool Overlaps (ColliderComponent other, out Contact contact) {
        contact = default;

        if (other is BoxColliderComponent otherBox) {
            return OverlapsBox(otherBox, out contact);
        }

        /// unknown collider pair; no narrow-phase test available yet
        return false;
    }

    private bool OverlapsBox (BoxColliderComponent other, out Contact contact) {
        contact = default;

        Bounds a = GetWorldBounds();
        Bounds b = other.GetWorldBounds();

        Vector3 overlap = new Vector3(
            MathF.Min(a.Max.X, b.Max.X) - MathF.Max(a.Min.X, b.Min.X),
            MathF.Min(a.Max.Y, b.Max.Y) - MathF.Max(a.Min.Y, b.Min.Y),
            MathF.Min(a.Max.Z, b.Max.Z) - MathF.Max(a.Min.Z, b.Min.Z)
        );

        if (overlap.X <= 0f || overlap.Y <= 0f || overlap.Z <= 0f) return false;

        /// resolve along the axis of least penetration
        if (overlap.X < overlap.Y && overlap.X < overlap.Z) {
            float sign = a.Center.X < b.Center.X ? -1f : 1f;
            contact = new Contact { normal = new Vector3(sign, 0f, 0f), penetration = overlap.X };
        } else if (overlap.Y < overlap.Z) {
            float sign = a.Center.Y < b.Center.Y ? -1f : 1f;
            contact = new Contact { normal = new Vector3(0f, sign, 0f), penetration = overlap.Y };
        } else {
            float sign = a.Center.Z < b.Center.Z ? -1f : 1f;
            contact = new Contact { normal = new Vector3(0f, 0f, sign), penetration = overlap.Z };
        }

        return true;
    }

}
