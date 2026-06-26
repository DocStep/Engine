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

    public static MeshData GenerateWireframe (float radius = 0.5f, float height = 1f, int lonSegments = 24) {
        var vertices = new List<Vertex>();
        var indices = new List<uint>();

        float halfHeight = 0.5f*height;

        /// Equator rings (top and bottom of the straight section).
        int topEquatorStart = vertices.Count;
        for (int lon = 0; lon < lonSegments; lon++) {
            float phi = 2f*MathF.PI*lon/lonSegments;
            vertices.Add(new Vertex(new Vector3(radius*MathF.Cos(phi), halfHeight, radius*MathF.Sin(phi)), Vector3.UnitY, Vector2.Zero));
        }

        int bottomEquatorStart = vertices.Count;
        for (int lon = 0; lon < lonSegments; lon++) {
            float phi = 2f*MathF.PI*lon/lonSegments;
            vertices.Add(new Vertex(new Vector3(radius*MathF.Cos(phi), -halfHeight, radius*MathF.Sin(phi)), Vector3.UnitY, Vector2.Zero));
        }

        for (int lon = 0; lon < lonSegments; lon++) {
            uint a = (uint)(topEquatorStart + lon);
            uint b = (uint)(topEquatorStart + (lon + 1)%lonSegments);
            indices.Add(a);
            indices.Add(b);

            uint c = (uint)(bottomEquatorStart + lon);
            uint d = (uint)(bottomEquatorStart + (lon + 1)%lonSegments);
            indices.Add(c);
            indices.Add(d);
        }

        /// Vertical struts between equators, every quarter turn.
        int strutStep = Math.Max(1, lonSegments/4);
        for (int lon = 0; lon < lonSegments; lon += strutStep) {
            indices.Add((uint)(topEquatorStart + lon));
            indices.Add((uint)(bottomEquatorStart + lon));
        }

        /// Two half-circle profile arcs (front XY-plane-ish, side ZY-plane-ish), each made of
        /// a top hemisphere arc + straight side + bottom hemisphere arc, as a single polyline.
        int arcSegments = Math.Max(4, lonSegments/2);
        for (int axis = 0; axis < 2; axis++) {
            var arc = new List<Vector3>();

            for (int i = 0; i <= arcSegments; i++) {
                float theta = MathF.PI*i/arcSegments - 0.5f*MathF.PI; /// -PI/2 .. PI/2 over the top half
                float c = MathF.Cos(theta);
                float s = MathF.Sin(theta);
                float px = axis == 0 ? radius*c : 0f;
                float pz = axis == 1 ? radius*c : 0f;
                arc.Add(new Vector3(px, radius*s + halfHeight, pz));
            }

            for (int i = 0; i <= arcSegments; i++) {
                float theta = MathF.PI*i/arcSegments - 0.5f*MathF.PI;
                float c = MathF.Cos(theta);
                float s = MathF.Sin(theta);
                float px = axis == 0 ? -radius*c : 0f;
                float pz = axis == 1 ? -radius*c : 0f;
                arc.Add(new Vector3(px, -radius*s - halfHeight, pz));
            }

            int arcStart = vertices.Count;
            foreach (var p in arc)
                vertices.Add(new Vertex(p, Vector3.UnitY, Vector2.Zero));

            for (int i = 0; i < arc.Count - 1; i++) {
                indices.Add((uint)(arcStart + i));
                indices.Add((uint)(arcStart + i + 1));
            }
        }

        return new MeshData(vertices.ToArray(), indices.ToArray());
    }

}