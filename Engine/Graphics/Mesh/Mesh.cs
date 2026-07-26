using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class Mesh : IAsset<Mesh> {
    public Mesh () {
        GL = Renderer.GL;
        Name = nameof(Mesh);
    }
    public Mesh (MeshData data) {
        GL = Renderer.GL;
        Name = nameof(Mesh);

        _indexCount = (uint)data.Indices.Length;
        Data = data;
        LocalAABB = AABB.FromVertices(data.Vertices);

        float[] vertices = Flatten(data.Vertices);

        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        _vbo = GL.GenBuffer();
        GL.BindBuffer(GLEnum.ArrayBuffer, _vbo);
        unsafe {
            fixed (float* v = vertices) {
                GL.BufferData(
                    GLEnum.ArrayBuffer,
                    (nuint)(vertices.Length*sizeof(float)),
                    v,
                    GLEnum.StaticDraw);
            }
        }

        _ebo = GL.GenBuffer();
        GL.BindBuffer(GLEnum.ElementArrayBuffer, _ebo);
        unsafe {
            fixed (uint* i = data.Indices) {
                GL.BufferData(
                    GLEnum.ElementArrayBuffer,
                    (nuint)(data.Indices.Length*sizeof(uint)),
                    i,
                    GLEnum.StaticDraw);
            }
        }

        const uint stride = Vertex.FloatStride*sizeof(float);
        unsafe {
            /// Position (location 0)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
            GL.EnableVertexAttribArray(0);

            /// Normal (location 1)
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (void*)(3*sizeof(float)));
            GL.EnableVertexAttribArray(1);

            /// UV (location 2)
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, (void*)(6*sizeof(float)));
            GL.EnableVertexAttribArray(2);
        }

        GL.BindVertexArray(0);
    }


    private readonly GL GL = null!;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _ebo;
    private readonly uint _indexCount;

    public readonly MeshData? Data;
    public readonly AABB LocalAABB;

    public string Name { get; private set; }


    private static float[] Flatten (Vertex[] verts) {
        float[] result = new float[verts.Length*Vertex.FloatStride];
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

    public void Draw (PrimitiveType primitiveType = PrimitiveType.Triangles) {
        GL.BindVertexArray(_vao);
        unsafe {
            GL.DrawElements(primitiveType, _indexCount, DrawElementsType.UnsignedInt, null);
        }
        Renderer.Instance.Stats.DrawCalls++;
        GL.BindVertexArray(0);
    }
    /// Non-indexed draw, used for fullscreen triangle / no vertex buffer
    public void Draw (uint vertexCount, PrimitiveType primitiveType = PrimitiveType.Triangles) {
        GL.BindVertexArray(_vao);

        GL.DrawArrays(primitiveType, 0, vertexCount);
        Renderer.Instance.Stats.DrawCalls++;

        GL.BindVertexArray(0);
    }

    public static Mesh Load (string path) {
        return new Mesh(ObjLoader.Load(path)) { Name = Path.GetFileNameWithoutExtension(path) };
    }


    public void Dispose () {
        GL.DeleteBuffer(_vbo);
        GL.DeleteBuffer(_ebo);
        GL.DeleteVertexArray(_vao);
    }

}