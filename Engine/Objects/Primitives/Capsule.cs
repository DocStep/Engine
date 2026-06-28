using System.Numerics;

namespace Engine.Graphics;


public static class Capsule {

    /// height is the distance between hemisphere centers (the straight cylindrical
    /// section); total capsule length is height + 2*radius.
    public static MeshData Generate (float radius = 0.5f, float height = 1f, int latSegments = 8, int lonSegments = 24) {
        var vertices = new List<Vertex>();
        var indices = new List<uint>();

        float halfHeight = 0.5f*height;
        int lonStride = lonSegments + 1;

        /// Top hemisphere: theta from 0 (pole) to PI/2 (equator), shifted up by halfHeight.
        int topStart = vertices.Count;
        for (int lat = 0; lat <= latSegments; lat++) {
            float theta = 0.5f*MathF.PI*lat/latSegments;
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);

            for (int lon = 0; lon <= lonSegments; lon++) {
                float phi = 2f*MathF.PI*lon/lonSegments;
                float sinPhi = MathF.Sin(phi);
                float cosPhi = MathF.Cos(phi);

                float x = cosPhi*sinTheta;
                float y = cosTheta;
                float z = sinPhi*sinTheta;

                var normal = new Vector3(x, y, z);
                var position = new Vector3(radius*x, radius*y + halfHeight, radius*z);
                var uv = new Vector2((float)lon/lonSegments, 1f - 0.5f*(float)lat/latSegments);

                vertices.Add(new Vertex(position, normal, uv));
            }
        }

        for (int lat = 0; lat < latSegments; lat++) {
            for (int lon = 0; lon < lonSegments; lon++) {
                uint first = (uint)(topStart + lat*lonStride + lon);
                uint second = (uint)(first + lonStride);

                indices.Add(first);
                indices.Add(first + 1);
                indices.Add(second);

                indices.Add(second);
                indices.Add(first + 1);
                indices.Add(second + 1);
            }
        }

        /// Bottom hemisphere: theta from PI/2 (equator) to PI (pole), shifted down by halfHeight.
        int bottomStart = vertices.Count;
        for (int lat = 0; lat <= latSegments; lat++) {
            float theta = 0.5f*MathF.PI + 0.5f*MathF.PI*lat/latSegments;
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);

            for (int lon = 0; lon <= lonSegments; lon++) {
                float phi = 2f*MathF.PI*lon/lonSegments;
                float sinPhi = MathF.Sin(phi);
                float cosPhi = MathF.Cos(phi);

                float x = cosPhi*sinTheta;
                float y = cosTheta;
                float z = sinPhi*sinTheta;

                var normal = new Vector3(x, y, z);
                var position = new Vector3(radius*x, radius*y - halfHeight, radius*z);
                var uv = new Vector2((float)lon/lonSegments, 0.5f - 0.5f*(float)lat/latSegments);

                vertices.Add(new Vertex(position, normal, uv));
            }
        }

        for (int lat = 0; lat < latSegments; lat++) {
            for (int lon = 0; lon < lonSegments; lon++) {
                uint first = (uint)(bottomStart + lat*lonStride + lon);
                uint second = (uint)(first + lonStride);

                indices.Add(first);
                indices.Add(first + 1);
                indices.Add(second);

                indices.Add(second);
                indices.Add(first + 1);
                indices.Add(second + 1);
            }
        }

        /// Connect the two equators (last ring of top hemisphere, first ring of bottom hemisphere).
        int topEquator = topStart + latSegments*lonStride;
        int bottomEquator = bottomStart;
        for (int lon = 0; lon < lonSegments; lon++) {
            uint a = (uint)(topEquator + lon);
            uint b = a + 1;
            uint c = (uint)(bottomEquator + lon);
            uint d = c + 1;

            indices.Add(a);
            indices.Add(b);
            indices.Add(c);

            indices.Add(c);
            indices.Add(b);
            indices.Add(d);
        }

        return new MeshData(vertices.ToArray(), indices.ToArray());
    }

    public static MeshData GenerateWireframe (float radius = 0.5f, int segments = 32) {
        Vector3 p1 = -0.5f*Vector3.UnitY;
        Vector3 p2 = 0.5f*Vector3.UnitY;
        var axis = p2-p1;
        var height = axis.Length();

        var up = height > 1e-6f ? axis/height : Vector3.UnitY;
        var (right, forward) = OrthonormalBasis(up);

        var vertices = new List<Vertex>();
        var indices = new List<uint>();

        /// Side lines connecting the two caps, at 4 points around the radius.
        for (int i = 0; i < 4; i++) {
            float angle = i*MathF.PI*0.5f;
            var offset = (right*MathF.Cos(angle)+forward*MathF.Sin(angle))*radius;
            AppendLine(vertices, indices, p1+offset, p2+offset);
        }

        /// Equatorial ring around each cap, perpendicular to the axis.
        Utils.AppendCircle(vertices, indices, p1, radius, segments, right, forward);
        Utils.AppendCircle(vertices, indices, p2, radius, segments, right, forward);

        /// Hemisphere caps: two perpendicular half-circles per end, bulging away from the body.
        Utils.AppendHalfCircle(vertices, indices, p1, radius, segments, right, up, flip: true);
        Utils.AppendHalfCircle(vertices, indices, p1, radius, segments, forward, up, flip: true);
        Utils.AppendHalfCircle(vertices, indices, p2, radius, segments, right, up, flip: false);
        Utils.AppendHalfCircle(vertices, indices, p2, radius, segments, forward, up, flip: false);

        return new MeshData(vertices.ToArray(), indices.ToArray());
    }

    private static void AppendLine (List<Vertex> vertices, List<uint> indices, Vector3 a, Vector3 b) {
        uint start = (uint)vertices.Count;
        vertices.Add(new Vertex { Position = a });
        vertices.Add(new Vertex { Position = b });
        indices.Add(start);
        indices.Add(start+1);
    }

    private static (Vector3 right, Vector3 forward) OrthonormalBasis (Vector3 up) {
        var reference = MathF.Abs(Vector3.Dot(up, Vector3.UnitY)) > 0.99f ? Vector3.UnitX : Vector3.UnitY;
        var right = Vector3.Normalize(Vector3.Cross(reference, up));
        var forward = Vector3.Cross(up, right);
        return (right, forward);
    }

}