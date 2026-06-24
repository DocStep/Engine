using System;
using System.Numerics;

namespace Engine.Graphics;


public class MeshComponent : Component {

    public Mesh? mesh = null;
    public Shader? shader = Renderer.Instance._sh_Lit;
    public Material? material = Renderer.Instance._m_Lit;
    public Silk.NET.OpenGL.PrimitiveType primitiveType = Silk.NET.OpenGL.PrimitiveType.Triangles;


}
