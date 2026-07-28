namespace Engine.Graphics;


public class MeshData {
    public Vertex[] Vertices;
    public uint[] Indices;
    public Silk.NET.OpenGL.PrimitiveType PrimitiveType = Silk.NET.OpenGL.PrimitiveType.Triangles;

    public MeshData (Vertex[] vertices, uint[] indices, Silk.NET.OpenGL.PrimitiveType primitiveType) {
        Vertices = vertices;
        Indices = indices;
        PrimitiveType = primitiveType;
    }
    public MeshData (MeshData data) {
        Vertices = new Vertex[data.Vertices.Length];
        Array.Copy(data.Vertices, Vertices, data.Vertices.Length);

        Indices = new uint[data.Indices.Length];
        Array.Copy(data.Indices, Indices, data.Indices.Length);

        PrimitiveType = data.PrimitiveType;
    }


    /// Recomputes per-vertex normals from triangle faces (area-weighted via
    /// the unnormalized cross product, then normalized once accumulated).
    /// Use this after import when a source file has no normals.
    public void RecalculateNormals () {
        if (PrimitiveType != Silk.NET.OpenGL.PrimitiveType.Triangles) return;

        for (int i = 0; i < Vertices.Length; i++)
            Vertices[i].Normal = Vector3.Zero;

        for (int i = 0; i < Indices.Length; i += 3) {
            uint ia = Indices[i];
            uint ib = Indices[i + 1];
            uint ic = Indices[i + 2];

            Vector3 a = Vertices[ia].Position;
            Vector3 b = Vertices[ib].Position;
            Vector3 c = Vertices[ic].Position;

            Vector3 faceNormal = Vector3.Cross(b - a, c - a);

            Vertices[ia].Normal += faceNormal;
            Vertices[ib].Normal += faceNormal;
            Vertices[ic].Normal += faceNormal;
        }

        for (int i = 0; i < Vertices.Length; i++) {
            Vector3 n = Vertices[i].Normal;
            if (0 < n.LengthSquared())
                Vertices[i].Normal = Vector3.Normalize(n);
        }
    }

    public void RecalculateOutlineNormals () {
        Dictionary<Vector3, Vector3> sums = new();

        for (int i = 0; i < Indices.Length; i += 3) {
            Vector3 a = Vertices[Indices[i]].Position;
            Vector3 b = Vertices[Indices[i + 1]].Position;
            Vector3 c = Vertices[Indices[i + 2]].Position;

            Vector3 normal = Vector3.Normalize(
                Vector3.Cross(b - a, c - a)
            );

            sums[a] = sums.GetValueOrDefault(a) + normal;
            sums[b] = sums.GetValueOrDefault(b) + normal;
            sums[c] = sums.GetValueOrDefault(c) + normal;
        }

        for (int i = 0; i < Vertices.Length; i++) {
            Vector3 pos = Vertices[i].Position;

            if (sums.TryGetValue(pos, out Vector3 normal))
                Vertices[i].Normal = Vector3.Normalize(normal);
        }
    }

}