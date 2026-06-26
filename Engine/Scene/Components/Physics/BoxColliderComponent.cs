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

    public OBB GetWorldOBB () {
        // Ignore local offsets as requested — just world transform.
        Vector3 worldPos = owner.Transform.Position;
        Vector3 worldScale = owner.Transform.Scale;

        Matrix4x4 rotMatrix = Utils.Rotation(owner.Transform.Rotation);  // Extension method — degrees in, matrix out

        return new OBB {
            Center      = worldPos,
            AxisX       = new Vector3(rotMatrix.M11, rotMatrix.M12, rotMatrix.M13),
            AxisY       = new Vector3(rotMatrix.M21, rotMatrix.M22, rotMatrix.M23),
            AxisZ       = new Vector3(rotMatrix.M31, rotMatrix.M32, rotMatrix.M33),
            HalfExtents = worldScale * 0.5f,
        };
    }

}
