namespace Engine.Graphics;


/// Plain CPU-side mesh data. No GL handles here on purpose —
/// procedural generators, the OBJ importer/exporter, and anything
/// else that builds or transforms geometry should only ever touch this.
/// Hand a finished MeshData to a Mesh when you actually need to draw it.
public class MeshData {
    public Vertex[] Vertices;
    public uint[] Indices;

    public MeshData (Vertex[] vertices, uint[] indices) {
        Vertices = vertices;
        Indices = indices;
    }

    /// Recomputes per-vertex normals from triangle faces (area-weighted via
    /// the unnormalized cross product, then normalized once accumulated).
    /// Use this after import when a source file has no normals.
    public void RecalculateNormals () {
        for (int i = 0; i < Vertices.Length; i++)
            Vertices[i].Normal = Silk.NET.Maths.Vector3D<float>.Zero;

        for (int i = 0; i < Indices.Length; i += 3) {
            uint ia = Indices[i];
            uint ib = Indices[i + 1];
            uint ic = Indices[i + 2];

            var a = Vertices[ia].Position;
            var b = Vertices[ib].Position;
            var c = Vertices[ic].Position;

            var faceNormal = Silk.NET.Maths.Vector3D.Cross(b - a, c - a);

            Vertices[ia].Normal += faceNormal;
            Vertices[ib].Normal += faceNormal;
            Vertices[ic].Normal += faceNormal;
        }

        for (int i = 0; i < Vertices.Length; i++) {
            var n = Vertices[i].Normal;
            if (n.LengthSquared > 0f)
                Vertices[i].Normal = Silk.NET.Maths.Vector3D.Normalize(n);
        }
    }
}