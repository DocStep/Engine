namespace Engine.Graphics;


public static class Sphere {

    public static MeshData Generate (float radius = 0.5f, int latSegments = 16, int lonSegments = 24) {
        List<Vertex> vertices = new List<Vertex>();
        List<uint> indices = new List<uint>();

        for (int lat = 0; lat <= latSegments; lat++) {
            float theta = MathF.PI*lat/latSegments; /// 0 (top) .. PI (bottom)
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);

            for (int lon = 0; lon <= lonSegments; lon++) {
                float phi = 2f*MathF.PI*lon/lonSegments; /// 0 .. 2PI
                float sinPhi = MathF.Sin(phi);
                float cosPhi = MathF.Cos(phi);

                float x = cosPhi*sinTheta;
                float y = cosTheta;
                float z = sinPhi*sinTheta;

                Vector3 position = new Vector3(radius*x, radius*y, radius*z);
                Vector3 normal = new Vector3(x, y, z);
                Vector2 uv = new Vector2((float)lon/lonSegments, 1f - (float)lat/latSegments);

                vertices.Add(new Vertex(position, normal, uv));
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

        return new MeshData(vertices.ToArray(), indices.ToArray(), Silk.NET.OpenGL.PrimitiveType.Triangles);
    }

    public static MeshData GenerateWireframe () {
        Vector3 center = Vector3.Zero;
        float radius = 0.5f;
        int segments = 32;
        List<Vertex> vertices = new List<Vertex>(segments*3);
        List<uint> indices = new List<uint>(segments*6);

        Utils.AppendCircle(vertices, indices, center, radius, segments, Axis.XY);
        Utils.AppendCircle(vertices, indices, center, radius, segments, Axis.XZ);
        Utils.AppendCircle(vertices, indices, center, radius, segments, Axis.YZ);

        return new MeshData(vertices.ToArray(), indices.ToArray(), Silk.NET.OpenGL.PrimitiveType.Lines);
    }

}