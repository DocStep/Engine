using Silk.NET.OpenGL;

namespace Engine;


public class WorldGrid : IDisposable {
    private readonly GL GL;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _vertexCount;

    public WorldGrid (int halfExtent = 10, float spacing = 1f) {
        GL = Graphics.Renderer.Instance.GL; ;

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

        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        _vbo = GL.GenBuffer();
        GL.BindBuffer(GLEnum.ArrayBuffer, _vbo);
        unsafe {
            var verticesArray = vertices.ToArray();
            fixed (float* v = verticesArray) {
                GL.BufferData(
                    GLEnum.ArrayBuffer,
                    (nuint)(verticesArray.Length * sizeof(float)),
                    v,
                    GLEnum.StaticDraw);
            }
        }

        unsafe {
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
            GL.EnableVertexAttribArray(0);
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
