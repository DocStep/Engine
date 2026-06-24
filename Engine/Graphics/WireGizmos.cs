using System.Numerics;

namespace Engine.Graphics;


public static class WireGizmos {

    public static MeshData Cube (Vector3 center, Vector3 size) {
        var half = size*0.5f;

        Span<Vector3> corners = stackalloc Vector3[8];
        corners[0] = center+new Vector3(-half.X, -half.Y, -half.Z);
        corners[1] = center+new Vector3(half.X, -half.Y, -half.Z);
        corners[2] = center+new Vector3(half.X, -half.Y, half.Z);
        corners[3] = center+new Vector3(-half.X, -half.Y, half.Z);
        corners[4] = center+new Vector3(-half.X, half.Y, -half.Z);
        corners[5] = center+new Vector3(half.X, half.Y, -half.Z);
        corners[6] = center+new Vector3(half.X, half.Y, half.Z);
        corners[7] = center+new Vector3(-half.X, half.Y, half.Z);

        var vertices = new Vertex[8];
        for (int i = 0; i < 8; i++)
            vertices[i] = new Vertex { Position = corners[i] };

        /// 12 edges, 2 indices each = 24 indices for GL_LINES.
        uint[] indices = [
            0, 1, 1, 2, 2, 3, 3, 0, /// bottom face
            4, 5, 5, 6, 6, 7, 7, 4, /// top face
            0, 4, 1, 5, 2, 6, 3, 7  /// vertical edges
        ];

        return new MeshData(vertices, indices);
    }

    /// Unity Gizmos.DrawWireSphere(center, radius) equivalent.
    /// Builds 3 great circles (XY, XZ, YZ planes) — same approach Unity uses internally.
    public static MeshData Sphere (Vector3 center, float radius, int segments = 32) {
        var vertices = new List<Vertex>(segments*3);
        var indices = new List<uint>(segments*6);

        AppendCircle(vertices, indices, center, radius, segments, Axis.XY);
        AppendCircle(vertices, indices, center, radius, segments, Axis.XZ);
        AppendCircle(vertices, indices, center, radius, segments, Axis.YZ);

        return new MeshData(vertices.ToArray(), indices.ToArray());
    }

    /// Unity Gizmos.DrawWireCapsule(p1, p2, radius) equivalent.
    /// p1/p2 are the centers of the two end caps (the capsule's central segment).
    public static MeshData Capsule (Vector3 p1, Vector3 p2, float radius, int segments = 32) {
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
        AppendCircle(vertices, indices, p1, radius, segments, right, forward);
        AppendCircle(vertices, indices, p2, radius, segments, right, forward);

        /// Hemisphere caps: two perpendicular half-circles per end, bulging away from the body.
        AppendHalfCircle(vertices, indices, p1, radius, segments, right, up, flip: true);
        AppendHalfCircle(vertices, indices, p1, radius, segments, forward, up, flip: true);
        AppendHalfCircle(vertices, indices, p2, radius, segments, right, up, flip: false);
        AppendHalfCircle(vertices, indices, p2, radius, segments, forward, up, flip: false);

        return new MeshData(vertices.ToArray(), indices.ToArray());
    }


    private enum Axis { XY, XZ, YZ }

    /// Axis-aligned circle (used by Sphere, where the basis is just the world axes).
    private static void AppendCircle (List<Vertex> vertices, List<uint> indices, Vector3 center, float radius, int segments, Axis axis) {
        (Vector3 u, Vector3 v) = axis switch {
            Axis.XY => (Vector3.UnitX, Vector3.UnitY),
            Axis.XZ => (Vector3.UnitX, Vector3.UnitZ),
            _ => (Vector3.UnitY, Vector3.UnitZ)
        };
        AppendCircle(vertices, indices, center, radius, segments, u, v);
    }

    /// Arbitrary-basis circle: center + radius*(cos(t)*u + sin(t)*v) for t in [0, 2*pi).
    private static void AppendCircle (List<Vertex> vertices, List<uint> indices, Vector3 center, float radius, int segments, Vector3 u, Vector3 v) {
        uint start = (uint)vertices.Count;

        for (int i = 0; i < segments; i++) {
            float t = i*MathF.Tau/segments;
            var pos = center+radius*(MathF.Cos(t)*u+MathF.Sin(t)*v);
            vertices.Add(new Vertex { Position = pos });
        }

        for (int i = 0; i < segments; i++) {
            indices.Add(start+(uint)i);
            indices.Add(start+(uint)((i+1)%segments));
        }
    }

    /// Half-circle in the plane spanned by (u, up), bulging toward +up if flip is false,
    /// toward -up if flip is true. Used for capsule hemisphere caps.
    private static void AppendHalfCircle (List<Vertex> vertices, List<uint> indices, Vector3 center, float radius, int segments, Vector3 u, Vector3 up, bool flip) {
        uint start = (uint)vertices.Count;
        var upDir = flip ? -up : up;
        int steps = Math.Max(segments/2, 2);

        for (int i = 0; i <= steps; i++) {
            float t = i*MathF.PI/steps;
            var pos = center+radius*(MathF.Cos(t)*u+MathF.Sin(t)*upDir);
            vertices.Add(new Vertex { Position = pos });
        }

        for (int i = 0; i < steps; i++) {
            indices.Add(start+(uint)i);
            indices.Add(start+(uint)(i+1));
        }
    }

    private static void AppendLine (List<Vertex> vertices, List<uint> indices, Vector3 a, Vector3 b) {
        uint start = (uint)vertices.Count;
        vertices.Add(new Vertex { Position = a });
        vertices.Add(new Vertex { Position = b });
        indices.Add(start);
        indices.Add(start+1);
    }

    /// Builds two vectors perpendicular to up (and to each other), avoiding
    /// degenerate cross products when up is near-parallel to UnitY.
    private static (Vector3 right, Vector3 forward) OrthonormalBasis (Vector3 up) {
        var reference = MathF.Abs(Vector3.Dot(up, Vector3.UnitY)) > 0.99f ? Vector3.UnitX : Vector3.UnitY;
        var right = Vector3.Normalize(Vector3.Cross(reference, up));
        var forward = Vector3.Cross(up, right);
        return (right, forward);
    }

}