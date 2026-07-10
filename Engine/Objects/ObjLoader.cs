using System.Globalization;

namespace Engine.Graphics;


public static class ObjLoader {
    public static MeshData Load (string path, Silk.NET.OpenGL.PrimitiveType primitiveType = Silk.NET.OpenGL.PrimitiveType.Triangles) {
        List<Vector3> positions = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();

        List<Vertex> vertices = new List<Vertex>();
        List<uint> indices = new List<uint>();
        Dictionary<(int, int, int), uint> cache = new Dictionary<(int, int, int), uint>();
        bool hasAnyNormal = false;

        foreach (string rawLine in File.ReadLines(path)) {
            string line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '#') continue;

            string[] tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) continue;

            switch (tokens[0]) {
                case "v":
                    positions.Add(new Vector3(
                        ParseFloat(tokens[1]), ParseFloat(tokens[2]), -ParseFloat(tokens[3])));
                    break;

                case "vt":
                    uvs.Add(new Vector2(ParseFloat(tokens[1]), ParseFloat(tokens[2])));
                    break;

                case "vn":
                    normals.Add(new Vector3(
                        ParseFloat(tokens[1]), ParseFloat(tokens[2]), -ParseFloat(tokens[3])));
                    hasAnyNormal = true;
                    break;

                case "f": {
                        uint[] faceIndices = new uint[tokens.Length - 1];
                        for (int i = 1; i < tokens.Length; i++)
                            faceIndices[i - 1] = ResolveVertex(tokens[i]);

                        for (int i = 1; i < faceIndices.Length - 1; i++) {
                            indices.Add(faceIndices[0]);
                            indices.Add(faceIndices[i + 1]);
                            indices.Add(faceIndices[i]);
                        }
                        break;
                    }
            }
        }

        MeshData data = new MeshData(vertices.ToArray(), indices.ToArray(), primitiveType);
        if (!hasAnyNormal)
            data.RecalculateNormals();

        return data;

        uint ResolveVertex (string token) {
            /// OBJ face vertex format: v, v/vt, v//vn, or v/vt/vn (1-based, negative = relative to end).
            string[] parts = token.Split('/');

            int vi = ParseObjIndex(parts[0], positions.Count);
            int ti = parts.Length > 1 && parts[1].Length > 0 ? ParseObjIndex(parts[1], uvs.Count) : -1;
            int ni = parts.Length > 2 && parts[2].Length > 0 ? ParseObjIndex(parts[2], normals.Count) : -1;

            (int vi, int ti, int ni) key = (vi, ti, ni);
            if (cache.TryGetValue(key, out uint existing))
                return existing;

            Vector3 position = positions[vi];
            Vector2 uv = 0 <= ti ? uvs[ti] : Vector2.Zero;
            Vector3 normal = ni >= 0 ? normals[ni] : Vector3.Zero;

            uint newIndex = (uint)vertices.Count;
            vertices.Add(new Vertex(position, normal, uv));
            cache[key] = newIndex;
            return newIndex;
        }
    }

    public static void Save (string path, MeshData data) {
        using StreamWriter writer = new StreamWriter(path);

        writer.WriteLine("# Exported by Engine.Graphics.ObjLoader");

        foreach (Vertex vert in data.Vertices)
            writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "v {0} {1} {2}", vert.Position.X, vert.Position.Y, vert.Position.Z));

        foreach (Vertex vert in data.Vertices)
            writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "vt {0} {1}", vert.UV.X, vert.UV.Y));

        foreach (Vertex vert in data.Vertices)
            writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "vn {0} {1} {2}", vert.Normal.X, vert.Normal.Y, vert.Normal.Z));

        /// OBJ indices are 1-based; position/uv/normal share the same index
        /// here since we write one of each per vertex.
        for (int i = 0; i < data.Indices.Length; i += 3) {
            uint a = data.Indices[i] + 1;
            uint b = data.Indices[i + 1] + 1;
            uint c = data.Indices[i + 2] + 1;
            writer.WriteLine($"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}");
        }
    }

    private static float ParseFloat (string s) => float.Parse(s, CultureInfo.InvariantCulture);

    /// OBJ indices are 1-based; negative indices count back from the end of the list so far.
    private static int ParseObjIndex (string s, int count) {
        int i = int.Parse(s, CultureInfo.InvariantCulture);
        return 0 < i ? i - 1 : count + i;
    }
}