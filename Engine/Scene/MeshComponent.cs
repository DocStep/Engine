using System;
using System.Numerics;

namespace Engine.Graphics;


public class MeshComponent : Component, IComponentUpdate {

    public Mesh? mesh = null;
    public Shader? shader = Renderer.Instance._sh_Lit;
    public Material? material = Renderer.Instance._m_Lit;
    public Silk.NET.OpenGL.PrimitiveType primitiveType = Silk.NET.OpenGL.PrimitiveType.Triangles;

    private RenderInfo renderInfo;


    public void Update () {
        if (mesh is null || shader is null || material is null) return;

        renderInfo = new RenderInfo() {
            pos = owner.position,
            rot = owner.rotation,
            scale = owner.scale,

            mesh = mesh,
            shader = shader,
            material = material,
            primitiveType = primitiveType,
        };
        Renderer.Instance.RenderList.Add(renderInfo);
    }

}
