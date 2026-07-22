using Silk.NET.OpenGL;

namespace Engine;


/// Three lines from the origin along +X (red), +Y (green), +Z (blue).
/// Vertices interleave position (xyz) + color (rgb) so axes render in one
/// draw call without needing per-axis uniform color swaps.
public class WorldAxes : IDisposable {
    private readonly GL GL;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _vertexCount;

    public WorldAxes (float length = 3f) {
        GL = Graphics.Renderer.GL;

        float[] vertices = {
            // Position           // Color (R,G,B)
            0f, 0f, 0f,           1f, 0f, 0f,
            length, 0f, 0f,       1f, 0f, 0f,

            0f, 0f, 0f,           0f, 1f, 0f,
            0f, length, 0f,       0f, 1f, 0f,

            0f, 0f, 0f,           0f, 0f, 1f,
            0f, 0f, length,       0f, 0f, 1f,
        };

        _vertexCount = (uint)(vertices.Length / 6);

        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        _vbo = GL.GenBuffer();
        GL.BindBuffer(GLEnum.ArrayBuffer, _vbo);
        unsafe {
            fixed (float* v = vertices) {
                GL.BufferData(
                    GLEnum.ArrayBuffer,
                    (nuint)(vertices.Length * sizeof(float)),
                    v,
                    GLEnum.StaticDraw);
            }
        }

        const uint floatsPerVertex = 6; // 3 position + 3 color
        unsafe {
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, floatsPerVertex * sizeof(float), (void*)0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, floatsPerVertex * sizeof(float), (void*)(3 * sizeof(float)));
            GL.EnableVertexAttribArray(1);
        }

        GL.BindVertexArray(0);
    }

    public void Draw () {
        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Lines, 0, _vertexCount);
        GL.BindVertexArray(0);
    }

    public void Dispose () {
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
    }
}
