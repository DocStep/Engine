namespace Engine.Graphics;


public struct RenderInfo () {
    public string name = string.Empty;
    /// Transform
    public Vector3 pos = Vector3.Zero;
    public Vector3 rot = Vector3.Zero;
    public Vector3 scale = Vector3.One;
    public Matrix4x4? modelOverride = null;

    /// MeshComponent
    public Mesh? mesh = null;
    public Material material = default!;
    public Silk.NET.OpenGL.PrimitiveType primitiveType = Silk.NET.OpenGL.PrimitiveType.Triangles;
    public float depthRangeNear = 0f;
    public float depthRangeFar = 1f;

    public Action? de_Pre = null;
    public Action? de_Post = null;

}
