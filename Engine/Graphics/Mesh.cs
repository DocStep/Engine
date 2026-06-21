using Silk.NET.OpenGL;

namespace Engine.Graphics;


/// GPU-side mesh. Owns the VAO/VBO/EBO and uploads a MeshData once on
/// construction. This is the single place attribute layout (location 0/1/2)
/// is set up, so every mesh in the engine — primitive or imported — draws
/// the same way.
public class Mesh : IDisposable {
    private readonly GL _gl;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _ebo;
    private readonly uint _indexCount;

    public Mesh (GL gl, MeshData data) {
        _gl = gl;
        _indexCount = (uint)data.Indices.Length;

        float[] vertices = Flatten(data.Vertices);

        _vao = _gl.GenVertexArray();
        _gl.BindVertexArray(_vao);

        _vbo = _gl.GenBuffer();
        _gl.BindBuffer(GLEnum.ArrayBuffer, _vbo);
        unsafe {
            fixed (float* v = vertices) {
                _gl.BufferData(
                    GLEnum.ArrayBuffer,
                    (nuint)(vertices.Length*sizeof(float)),
                    v,
                    GLEnum.StaticDraw);
            }
        }

        _ebo = _gl.GenBuffer();
        _gl.BindBuffer(GLEnum.ElementArrayBuffer, _ebo);
        unsafe {
            fixed (uint* i = data.Indices) {
                _gl.BufferData(
                    GLEnum.ElementArrayBuffer,
                    (nuint)(data.Indices.Length*sizeof(uint)),
                    i,
                    GLEnum.StaticDraw);
            }
        }

        const uint stride = Vertex.FloatStride*sizeof(float);
        unsafe {
            /// Position (location 0)
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
            _gl.EnableVertexAttribArray(0);

            /// Normal (location 1)
            _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (void*)(3*sizeof(float)));
            _gl.EnableVertexAttribArray(1);

            /// UV (location 2)
            _gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, (void*)(6*sizeof(float)));
            _gl.EnableVertexAttribArray(2);
        }

        _gl.BindVertexArray(0);
    }

    private static float[] Flatten (Vertex[] verts) {
        var result = new float[verts.Length*Vertex.FloatStride];
        for (int i = 0; i < verts.Length; i++) {
            int o = i*(int)Vertex.FloatStride;
            result[o + 0] = verts[i].Position.X;
            result[o + 1] = verts[i].Position.Y;
            result[o + 2] = verts[i].Position.Z;
            result[o + 3] = verts[i].Normal.X;
            result[o + 4] = verts[i].Normal.Y;
            result[o + 5] = verts[i].Normal.Z;
            result[o + 6] = verts[i].UV.X;
            result[o + 7] = verts[i].UV.Y;
        }
        return result;
    }

    public void Draw () {
        _gl.BindVertexArray(_vao);
        unsafe {
            _gl.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, null);
        }
        _gl.BindVertexArray(0);
    }

    public void Dispose () {
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
        _gl.DeleteVertexArray(_vao);
    }
}