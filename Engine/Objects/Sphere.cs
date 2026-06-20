using Silk.NET.OpenGL;

namespace Engine;


/// Generated UV sphere with interleaved position (xyz) + normal (xyz) vertices.
/// For a unit-radius sphere, the normal at each vertex equals its position, so
/// no separate normal pass is needed.
public class Sphere : IDisposable {
    private readonly GL _gl;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _ebo;
    private readonly uint _indexCount;

    public Sphere (GL gl, int latSegments = 16, int lonSegments = 24) {
        _gl = gl;

        var vertices = new List<float>();
        var indices = new List<uint>();

        for (int lat = 0; lat <= latSegments; lat++) {
            float theta = MathF.PI * lat / latSegments; // 0 (top) .. PI (bottom)
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);

            for (int lon = 0; lon <= lonSegments; lon++) {
                float phi = 2f * MathF.PI * lon / lonSegments; // 0 .. 2PI
                float sinPhi = MathF.Sin(phi);
                float cosPhi = MathF.Cos(phi);

                float x = cosPhi * sinTheta;
                float y = cosTheta;
                float z = sinPhi * sinTheta;

                // Position (unit sphere — scale to radius via uModel)
                vertices.Add(x);
                vertices.Add(y);
                vertices.Add(z);
                // Normal (same as position for a unit sphere centered at origin)
                vertices.Add(x);
                vertices.Add(y);
                vertices.Add(z);
            }
        }

        int stride = lonSegments + 1;
        for (int lat = 0; lat < latSegments; lat++) {
            for (int lon = 0; lon < lonSegments; lon++) {
                uint first = (uint)(lat*stride + lon);
                uint second = (uint)(first + stride);

                indices.Add(first);
                indices.Add(first + 1);
                indices.Add(second);

                indices.Add(second);
                indices.Add(first + 1);
                indices.Add(second + 1);
            }
        }

        _indexCount = (uint)indices.Count;

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

        _ebo = _gl.GenBuffer();
        _gl.BindBuffer(GLEnum.ElementArrayBuffer, _ebo);
        unsafe {
            var indicesArray = indices.ToArray();
            fixed (uint* i = indicesArray) {
                _gl.BufferData(
                    GLEnum.ElementArrayBuffer,
                    (nuint)(indicesArray.Length * sizeof(uint)),
                    i,
                    GLEnum.StaticDraw);
            }
        }

        const uint floatsPerVertex = 6; // 3 position + 3 normal
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