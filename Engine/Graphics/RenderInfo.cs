namespace Engine.Graphics;


public struct RenderInfo () {
    public string name = string.Empty;
    /// Transform
    public Vector3 pos = Vector3.Zero;
    public Quaternion rot = Quaternion.Identity;
    public Vector3 rotEuler { 
        set => rot = Utils.QuaternionFromEuler(Utils.WrapVector3(value, 0, 360));
    }
    public Vector3 scale = Vector3.One;
    public Matrix4x4? modelOverride = null;

    /// MeshComponent
    public Mesh mesh = null!;
    public Material material = null!;
    public Silk.NET.OpenGL.PrimitiveType primitiveType = Silk.NET.OpenGL.PrimitiveType.Triangles;
    public float depthRangeNear = 0f;
    public float depthRangeFar = 1f;

    public Action? de_Pre = null;
    public Action? de_Post = null;

}
