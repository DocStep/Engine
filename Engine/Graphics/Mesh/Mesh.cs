using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class Mesh : IAsset<Mesh>, IOnLoaded {
    public Mesh () {
        GL = Renderer.GL;
    }
    [Newtonsoft.Json.JsonConstructor]
    public Mesh (bool deserializing) {
        if (Data is null) return;
        GL = Renderer.GL;
    }
    [System.Runtime.Serialization.OnDeserialized]
    public void OnLoaded () {
        if (Data is not null) Upload(Data);
    }
    public Mesh (MeshData data) {
        GL = Renderer.GL;
        Upload(data);
    }


    public string Name { get; set; } = nameof(Mesh);
    public long Id { get; set; }
    public string? Path { get; set; }

    private readonly GL GL = null!;
    private uint _vao;
    private uint _vbo;
    private uint _ebo;
    private uint _indexCount;

    public MeshData? Data;
    public AABB LocalAABB;

    /// Instancing — set up lazily on first DrawInstanced() call so meshes that are never
    /// instanced don't pay for the extra buffer/attribute setup.
    private uint _instanceVbo;
    private int _instanceCapacity = -1; /// -1 = EnsureInstanceBuffer() not yet called
    private float[] _instanceUploadScratch = Array.Empty<float>(); /// grows, never shrinks — avoids a per-draw heap alloc


    private void Upload (MeshData data) {
        Data = data;
        //Log.log(Name, Data.Vertices.Length, Data.Indices.Length);
        _indexCount = (uint)Data.Indices.Length;
        LocalAABB = AABB.FromVertices(Data.Vertices);

        float[] vertices = Flatten(Data.Vertices);

        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        _vbo = GL.GenBuffer();
        GL.BindBuffer(GLEnum.ArrayBuffer, _vbo);
        unsafe {
            fixed (float* v = vertices) {
                GL.BufferData(GLEnum.ArrayBuffer, (nuint)(vertices.Length*sizeof(float)), v, GLEnum.StaticDraw);
            }
        }

        _ebo = GL.GenBuffer();
        GL.BindBuffer(GLEnum.ElementArrayBuffer, _ebo);
        unsafe {
            fixed (uint* i = Data.Indices) {
                GL.BufferData(GLEnum.ElementArrayBuffer, (nuint)(Data.Indices.Length*sizeof(uint)), i, GLEnum.StaticDraw);
            }
        }

        const uint stride = Vertex.FloatStride*sizeof(float);
        unsafe {
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (void*)(3*sizeof(float)));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, (void*)(6*sizeof(float)));
            GL.EnableVertexAttribArray(2);
        }

        GL.BindVertexArray(0);
    }

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

    /// Draws `models.Length` copies of this mesh in a single draw call. The shader assigned to
    /// the material used for this draw MUST read the model/normal matrix from the instanced
    /// vertex attributes (location 3 = model mat4, location 7 = normal mat4 — see the
    /// *_instanced shader variants) instead of the uModel/uNormalMatrix uniforms, or every
    /// instance renders with garbage/zeroed transforms.
    public void DrawInstanced (ReadOnlySpan<Matrix4x4> models, ReadOnlySpan<Matrix4x4> normals, PrimitiveType primitiveType = PrimitiveType.Triangles) {
        int instanceCount = models.Length;
        if (instanceCount == 0) return;

        EnsureInstanceBuffer();

        const int floatsPerInstance = 32; /// mat4 model (16) + mat4 normal (16)
        int floatCount = instanceCount*floatsPerInstance;
        if (_instanceUploadScratch.Length < floatCount) _instanceUploadScratch = new float[floatCount];

        for (int i = 0; i < instanceCount; i++) {
            int o = i*floatsPerInstance;
            WriteMatrix(_instanceUploadScratch, o, models[i]);
            WriteMatrix(_instanceUploadScratch, o + 16, normals[i]);
        }

        GL.BindVertexArray(_vao);
        GL.BindBuffer(GLEnum.ArrayBuffer, _instanceVbo);

        unsafe {
            fixed (float* b = _instanceUploadScratch) {
                if (_instanceCapacity < instanceCount) {
                    GL.BufferData(GLEnum.ArrayBuffer, (nuint)(floatCount*sizeof(float)), b, GLEnum.DynamicDraw);
                    _instanceCapacity = instanceCount;
                } else {
                    GL.BufferSubData(GLEnum.ArrayBuffer, 0, (nuint)(floatCount*sizeof(float)), b);
                }
            }

            GL.DrawElementsInstanced(primitiveType, _indexCount, DrawElementsType.UnsignedInt, null, (uint)instanceCount);
        }

        Renderer.Instance.Stats.DrawCalls++;
        GL.BindVertexArray(0);
    }

    /// A mat4 vertex attribute consumes 4 consecutive locations (one vec4 per column), so the
    /// model matrix occupies 3-6 and the normal matrix occupies 7-10. Divisor 1 on every column
    /// makes them advance once per instance instead of once per vertex.
    private void EnsureInstanceBuffer () {
        if (0 <= _instanceCapacity) return;

        _instanceVbo = GL.GenBuffer();
        _instanceCapacity = 0;

        GL.BindVertexArray(_vao);
        GL.BindBuffer(GLEnum.ArrayBuffer, _instanceVbo);

        const uint mat4Size = 16*sizeof(float);
        const uint instanceStride = mat4Size*2;

        unsafe {
            for (uint col = 0; col < 4; col++) {
                uint loc = 3 + col;
                GL.VertexAttribPointer(loc, 4, VertexAttribPointerType.Float, false, instanceStride, (void*)(col*4*sizeof(float)));
                GL.EnableVertexAttribArray(loc);
                GL.VertexAttribDivisor(loc, 1);
            }
            for (uint col = 0; col < 4; col++) {
                uint loc = 7 + col;
                GL.VertexAttribPointer(loc, 4, VertexAttribPointerType.Float, false, instanceStride, (void*)(mat4Size + col*4*sizeof(float)));
                GL.EnableVertexAttribArray(loc);
                GL.VertexAttribDivisor(loc, 1);
            }
        }

        GL.BindVertexArray(0);
    }

    private static void WriteMatrix (float[] dst, int offset, Matrix4x4 m) {
        dst[offset + 0] = m.M11; dst[offset + 1] = m.M12; dst[offset + 2] = m.M13; dst[offset + 3] = m.M14;
        dst[offset + 4] = m.M21; dst[offset + 5] = m.M22; dst[offset + 6] = m.M23; dst[offset + 7] = m.M24;
        dst[offset + 8] = m.M31; dst[offset + 9] = m.M32; dst[offset + 10] = m.M33; dst[offset + 11] = m.M34;
        dst[offset + 12] = m.M41; dst[offset + 13] = m.M42; dst[offset + 14] = m.M43; dst[offset + 15] = m.M44;
    }


    public void Save (string path) {
        if (Data is null) return;
        ObjLoader.Save(path, Data);
    }

    public static Mesh Load (string path) {
        return new Mesh(ObjLoader.Load(path)) {
            Name = System.IO.Path.GetFileNameWithoutExtension(path),
            Path = path,
        };
    }



    public void Dispose () {
        GL.DeleteBuffer(_vbo);
        GL.DeleteBuffer(_ebo);
        if (0 <= _instanceCapacity) GL.DeleteBuffer(_instanceVbo);
        GL.DeleteVertexArray(_vao);
    }

}