using System;
using System.Numerics;

namespace Engine.Graphics;


public class BoxColliderComponent : ColliderComponent {

    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;

    private RenderInfo renderInfo;


    public override void FixedUpdate () {

    }
    public override void Update () {
        renderInfo = new RenderInfo() {
            pos = owner.position,
            rot = owner.rotation,
            scale = owner.scale,

            mesh = Renderer.Instance._mesh_GizmoCube,
            shader = Renderer.Instance._sh_Unlit,
            material = Renderer.Instance._mat_GizmosG,
            primitiveType = Silk.NET.OpenGL.PrimitiveType.Lines,
        };
        Renderer.Instance.RenderList.Add(renderInfo);
    }



}
