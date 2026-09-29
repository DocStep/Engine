using Silk.NET.OpenGL;

namespace Engine.Graphics;


public sealed unsafe class ParticleRenderer {

    public Mesh QuadMesh; /// a shared unit quad, 4 verts / 6 indices, reused by every system
    public Material Material;

    uint _instanceVbo;
    int _capacity;

    static GL GL => Renderer.GL;

    public ParticleRenderer (Mesh quadMesh, Material material, int capacity) {
        QuadMesh = quadMesh;
        Material = material;
        _capacity = capacity;

        _instanceVbo = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, _instanceVbo);
        GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(capacity*sizeof(ParticleInstance)), null, BufferUsageARB.StreamDraw);

        /// Attach the instance stream to the quad's VAO — locations 3/4 are free since the
        /// scene instancing path (Mesh.DrawInstanced) uses 3/7 for model/normal matrices;
        /// this VAO is only ever bound for particles, so there's no actual clash, but keep
        /// them distinct if you ever share a VAO between the two paths
        GL.BindVertexArray(QuadMesh.VAO);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, _instanceVbo);

        int stride = sizeof(ParticleInstance);
        GL.EnableVertexAttribArray(3);
        GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, (uint)stride, (void*)0); /// xyz = pos, w = scale
        GL.VertexAttribDivisor(3, 1);

        GL.EnableVertexAttribArray(4);
        GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, (uint)stride, (void*)16); /// rgba
        GL.VertexAttribDivisor(4, 1);

        GL.BindVertexArray(0);
    }

    public void Draw (ReadOnlySpan<ParticleInstance> instances) {
        if (instances.Length == 0) return;
        if (instances.Length > _capacity) Grow(instances.Length);

        GL.BindBuffer(BufferTargetARB.ArrayBuffer, _instanceVbo);
        /// Orphan: ask for a fresh block instead of writing into one the GPU might still be
        /// reading from last frame's draw — avoids a CPU/GPU sync stall
        GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(_capacity*sizeof(ParticleInstance)), null, BufferUsageARB.StreamDraw);
        fixed (ParticleInstance* ptr = instances)
            GL.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)(instances.Length*sizeof(ParticleInstance)), ptr);

        Renderer.Instance.BindMaterial(Material); /// see note below — needs to be accessible
        GL.BindVertexArray(QuadMesh.VAO);
        GL.DrawElementsInstanced(PrimitiveType.Triangles, (uint)QuadMesh.IndexCount, DrawElementsType.UnsignedInt, null, (uint)instances.Length);
        GL.BindVertexArray(0);
    }

    void Grow (int newCount) {
        _capacity = newCount;
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, _instanceVbo);
        GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(_capacity*sizeof(ParticleInstance)), null, BufferUsageARB.StreamDraw);
    }

}