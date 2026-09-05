namespace Engine.Graphics;


public struct RenderInfo () {

    public Matrix4x4 model;
    public Matrix4x4? normal = null;

    public Mesh mesh = null!;
    public Material material = null!;
    public Silk.NET.OpenGL.PrimitiveType primitiveType = Silk.NET.OpenGL.PrimitiveType.Triangles;

}
