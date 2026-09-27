namespace Engine.Graphics;


public struct RenderInfo () {

    public Matrix4x4 model;
    public Matrix4x4? normal = null;

    public Mesh mesh = null!;
    public Material material = null!;
    public Silk.NET.OpenGL.PrimitiveType primitiveType = Silk.NET.OpenGL.PrimitiveType.Triangles;
    
    /// Which slice of the mesh's index buffer to draw. indexCount == 0 means "whole mesh" —
    /// DrawRenderInfo/DrawInstancedRun fall back to mesh._indexCount in that case.
    public uint indexOffset = 0;
    public uint indexCount = 0;

}
