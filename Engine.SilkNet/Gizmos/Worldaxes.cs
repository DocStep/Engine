using Silk.NET.OpenGL;

namespace Engine.SilkNet;


/// Three lines from the origin along +X (red), +Y (green), +Z (blue).
/// Vertices interleave position (xyz) + color (rgb) so axes render in one
/// draw call without needing per-axis uniform color swaps.
public class WorldAxes : IDisposable {
    private readonly GL _gl;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _vertexCount;

    public WorldAxes (GL gl, float length = 3f) {
        _gl = gl;

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

        _vao = _gl.GenVertexArray();
        _gl.BindVertexArray(_vao);

        _vbo = _gl.GenBuffer();
        _gl.BindBuffer(GLEnum.ArrayBuffer, _vbo);
        unsafe {
            fixed (float* v = vertices) {
                _gl.BufferData(
                    GLEnum.ArrayBuffer,
                    (nuint)(vertices.Length * sizeof(float)),
                    v,
                    GLEnum.StaticDraw);
            }
        }

        const uint floatsPerVertex = 6; // 3 position + 3 color
        unsafe {
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, floatsPerVertex * sizeof(float), (void*)0);
            _gl.EnableVertexAttribArray(0);

            _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, floatsPerVertex * sizeof(float), (void*)(3 * sizeof(float)));
            _gl.EnableVertexAttribArray(1);
        }

        _gl.BindVertexArray(0);
    }

    public void Draw () {
        _gl.BindVertexArray(_vao);
        _gl.DrawArrays(PrimitiveType.Lines, 0, _vertexCount);
        _gl.BindVertexArray(0);
    }

    public void Dispose () {
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteVertexArray(_vao);
    }
}
