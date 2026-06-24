using System;
using System.Numerics;

namespace Engine.Graphics;


public struct RenderInfo () {
    /// Transform
    public Vector3 pos = default;
    public Vector3 rot = default;
    public Vector3 scale = default;
    /// MeshComponent
    public Mesh mesh = default!;
    public Shader shader = default!;
    public Material material = default!;
    public Silk.NET.OpenGL.PrimitiveType primitiveType = Silk.NET.OpenGL.PrimitiveType.Triangles;
}
