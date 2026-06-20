using Silk.NET.OpenGL;

namespace Engine;

public class Cube : IDisposable {
    private readonly GL _gl;

    private uint _vao;
    private uint _vbo;
    private uint _ebo;

    // Position Normal
    private readonly float[] _vertices = {
        /// Front
        -0.5f, -0.5f,  0.5f,   0f, 0f, 1f,
         0.5f, -0.5f,  0.5f,   0f, 0f, 1f,
         0.5f,  0.5f,  0.5f,   0f, 0f, 1f,
        -0.5f,  0.5f,  0.5f,   0f, 0f, 1f,

        /// Back
        -0.5f, -0.5f, -0.5f,   0f, 0f,-1f,
         0.5f, -0.5f, -0.5f,   0f, 0f,-1f,
         0.5f,  0.5f, -0.5f,   0f, 0f,-1f,
        -0.5f,  0.5f, -0.5f,   0f, 0f,-1f,

        // Left face
        -0.5f, -0.5f, -0.5f,  -1f, 0f, 0f,
        -0.5f, -0.5f,  0.5f,  -1f, 0f, 0f,
        -0.5f,  0.5f,  0.5f,  -1f, 0f, 0f,
        -0.5f,  0.5f, -0.5f,  -1f, 0f, 0f,

        /// Right
         0.5f, -0.5f, -0.5f,   1f, 0f, 0f,
         0.5f, -0.5f,  0.5f,   1f, 0f, 0f,
         0.5f,  0.5f,  0.5f,   1f, 0f, 0f,
         0.5f,  0.5f, -0.5f,   1f, 0f, 0f,

        /// Top
        -0.5f,  0.5f,  0.5f,   0f, 1f, 0f,
         0.5f,  0.5f,  0.5f,   0f, 1f, 0f,
         0.5f,  0.5f, -0.5f,   0f, 1f, 0f,
        -0.5f,  0.5f, -0.5f,   0f, 1f, 0f,

        /// Bottom
        -0.5f, -0.5f,  0.5f,   0f,-1f, 0f,
         0.5f, -0.5f,  0.5f,   0f,-1f, 0f,
         0.5f, -0.5f, -0.5f,   0f,-1f, 0f,
        -0.5f, -0.5f, -0.5f,   0f,-1f, 0f,
    };
    private readonly uint[] _indices = {
        0, 1, 2,  2, 3, 0,       // front
        4, 6, 5,  6, 4, 7,       // back
        8, 9, 10, 10, 11, 8,     // left
        12, 14, 13, 14, 12, 15,  // right
        16, 17, 18, 18, 19, 16,  // top
        20, 22, 21, 22, 20, 23,  // bottom
    };

    public Cube (GL gl) {
        _gl = gl;
        Setup();
    }

    private unsafe void Setup () {
        _vao = _gl.GenVertexArray();
        _gl.BindVertexArray(_vao);

        _vbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (float* v = _vertices) {
            _gl.BufferData(BufferTargetARB.ArrayBuffer,
                (nuint)(_vertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
        }

        _ebo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        fixed (uint* i = _indices) {
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer,
                (nuint)(_indices.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw);
        }

        const uint stride = 6 * sizeof(float);

        // Position attribute (location 0)
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
        _gl.EnableVertexAttribArray(0);

        // Color attribute (location 1)
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
        _gl.EnableVertexAttribArray(1);
    }

    public unsafe void Draw () {
        _gl.BindVertexArray(_vao);
        _gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
    }

    public void Dispose () {
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
        _gl.DeleteVertexArray(_vao);
    }
}