namespace Engine.Graphics;


public struct RenderData2D () {

    public Matrix4x4 model;

    public Mesh mesh = null!;
    public Material material = null!;
    public Silk.NET.OpenGL.PrimitiveType primitiveType = Silk.NET.OpenGL.PrimitiveType.Triangles;

}
