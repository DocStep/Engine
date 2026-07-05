namespace Engine.Graphics;


public struct RenderInfo () {

    /// Transform
    public Vector3 pos = Vector3.Zero;
    public Vector3 rot = Vector3.Zero;
    public Vector3 scale = Vector3.One;

    /// MeshComponent
    public Mesh? mesh = null;
    public Shader shader = default!;
    public Material material = default!;
    public Silk.NET.OpenGL.PrimitiveType primitiveType = Silk.NET.OpenGL.PrimitiveType.Triangles;

}
