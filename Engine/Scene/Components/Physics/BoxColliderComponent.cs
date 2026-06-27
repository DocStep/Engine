using System;
using System.Numerics;
using System.Collections.Generic;

namespace Engine;


public class BoxColliderComponent : ColliderComponent {

    public Vector3 position = Vector3.Zero;
    public Vector3 rotation = Vector3.Zero;
    public Vector3 scale = Vector3.One;


    public override void Update () {
        if (drawGizmos) {
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

        /*PhysicsComponent? physicsComponent = owner.GetComponent<PhysicsComponent>();
        if (physicsComponent is not null && !physicsComponent.isKinematic) {
            Graphics.UI.TextRenderer.AddText(string.Empty);
            string vel = $"{owner.Name} Vel: {physicsComponent.Velocity}";
            string velAng = $"{owner.Name} VelAng: {physicsComponent.VelocityAngular}";
            string size = $"{owner.Name} HalfExtents: {GetWorldOBB().HalfExtents}";
            Graphics.UI.TextRenderer.AddText(vel);
            Graphics.UI.TextRenderer.AddText(velAng);
            Graphics.UI.TextRenderer.AddText(size);
            //Log.log(vel);
            //Log.log(velAng);
            //Log.log(size);
        }*/
    }

}
