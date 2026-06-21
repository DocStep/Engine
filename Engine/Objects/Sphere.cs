using Silk.NET.Maths;

namespace Engine.Graphics;


/// Generates a UV sphere as MeshData. No GL here — wrap the result in a
/// Mesh to actually draw it: new Mesh(gl, Sphere.Generate()).
public static class Sphere {
    public static MeshData Generate (float radius = 0.5f, int latSegments = 16, int lonSegments = 24) {
        var vertices = new List<Vertex>();
        var indices = new List<uint>();

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

                var position = new Vector3D<float>(radius*x, radius*y, radius*z);
                var normal = new Vector3D<float>(x, y, z);
                var uv = new Vector2D<float>((float)lon/lonSegments, 1f - (float)lat/latSegments);

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

        return new MeshData(vertices.ToArray(), indices.ToArray());
    }
}