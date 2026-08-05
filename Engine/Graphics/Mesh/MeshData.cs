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
        // Accumulate unnormalized face normals (area-weighted) per unique position
        // and normalize once at the end. This avoids using individually normalized
        // face normals which can cause visible seams when faces are offset for
        // outline rendering.
        Dictionary<Vector3, Vector3> sums = new();

        for (int i = 0; i < Indices.Length; i += 3) {
            Vector3 a = Vertices[Indices[i]].Position;
            Vector3 b = Vertices[Indices[i + 1]].Position;
            Vector3 c = Vertices[Indices[i + 2]].Position;

            // Use the unnormalized cross product so larger faces contribute more
            // to the vertex normal (area-weighted). This yields smoother, more
            // stable normals for the outline offset.
            Vector3 faceNormal = Vector3.Cross(b - a, c - a);

            if (sums.TryGetValue(a, out var sa)) sums[a] = sa + faceNormal; else sums[a] = faceNormal;
            if (sums.TryGetValue(b, out var sb)) sums[b] = sb + faceNormal; else sums[b] = faceNormal;
            if (sums.TryGetValue(c, out var sc)) sums[c] = sc + faceNormal; else sums[c] = faceNormal;
        }

        for (int i = 0; i < Vertices.Length; i++) {
            Vector3 pos = Vertices[i].Position;

            if (sums.TryGetValue(pos, out Vector3 normal) && normal.LengthSquared() > 0f)
                Vertices[i].Normal = Vector3.Normalize(normal);
        }
    }

    /// Weld vertices that share the same position (within an epsilon) into a single
    /// vertex so that outline offsets remain connected. Returns a new MeshData
    /// instance with remapped indices.
    public MeshData Weld (float epsilon = 1e-5f) {
        if (Vertices.Length == 0) return new MeshData(new Vertex[0], new uint[0], PrimitiveType);

        var map = new Dictionary<(long, long, long), int>();
        var newVerts = new List<Vertex>();
        uint[] newIndices = new uint[Indices.Length];

        long Quantize(float v) => (long)Math.Round(v/epsilon);

        for (int i = 0; i < Vertices.Length; i++) {
            var p = Vertices[i].Position;
            var key = (Quantize(p.X), Quantize(p.Y), Quantize(p.Z));

            if (!map.TryGetValue(key, out int idx)) {
                idx = newVerts.Count;
                map[key] = idx;
                // copy vertex (keep first encountered attributes)
                newVerts.Add(Vertices[i]);
            }
        }

        for (int i = 0; i < Indices.Length; i++) {
            uint oldIndex = Indices[i];
            var p = Vertices[oldIndex].Position;
            var key = (Quantize(p.X), Quantize(p.Y), Quantize(p.Z));
            newIndices[i] = (uint)map[key];
        }

        return new MeshData(newVerts.ToArray(), newIndices, PrimitiveType);
    }

}