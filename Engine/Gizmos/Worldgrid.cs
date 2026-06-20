using Silk.NET.OpenGL;

namespace Engine;


/// Flat grid of lines on the XZ plane, centered at the origin.
/// `halfExtent` is the number of cells in each direction from center;
/// `spacing` is the distance between adjacent lines.
public class WorldGrid : IDisposable {
    private readonly GL _gl;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _vertexCount;

    public WorldGrid (GL gl, int halfExtent = 10, float spacing = 1f) {
        _gl = gl;

        var vertices = new List<float>();
        float extent = halfExtent * spacing;

        for (int i = -halfExtent; i <= halfExtent; i++) {
            float offset = i * spacing;

            // Line parallel to X axis (varying X, fixed Z)
            vertices.Add(-extent); vertices.Add(0f); vertices.Add(offset);
            vertices.Add(extent); vertices.Add(0f); vertices.Add(offset);

            // Line parallel to Z axis (fixed X, varying Z)
            vertices.Add(offset); vertices.Add(0f); vertices.Add(-extent);
            vertices.Add(offset); vertices.Add(0f); vertices.Add(extent);
        }

        _vertexCount = (uint)(vertices.Count / 3);

        _vao = _gl.GenVertexArray();
        _gl.BindVertexArray(_vao);

        _vbo = _gl.GenBuffer();
        _gl.BindBuffer(GLEnum.ArrayBuffer, _vbo);
        unsafe {
            var verticesArray = vertices.ToArray();
            fixed (float* v = verticesArray) {
                _gl.BufferData(
                    GLEnum.ArrayBuffer,
                    (nuint)(verticesArray.Length * sizeof(float)),
                    v,
                    GLEnum.StaticDraw);
            }
        }

        unsafe {
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
            _gl.EnableVertexAttribArray(0);
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
