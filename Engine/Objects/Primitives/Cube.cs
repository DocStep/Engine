namespace Engine.Graphics;


public static class Cube {

    public static MeshData Generate (float size = 1f) {
        float h = 0.5f*size;

        Vertex[] vertices = {
            /// Front
            new(new(-h, -h,  h), new(0f, 0f, 1f), new(0f, 0f)),
            new(new( h, -h,  h), new(0f, 0f, 1f), new(1f, 0f)),
            new(new( h,  h,  h), new(0f, 0f, 1f), new(1f, 1f)),
            new(new(-h,  h,  h), new(0f, 0f, 1f), new(0f, 1f)),

            /// Back
            new(new(-h, -h, -h), new(0f, 0f, -1f), new(1f, 0f)),
            new(new( h, -h, -h), new(0f, 0f, -1f), new(0f, 0f)),
            new(new( h,  h, -h), new(0f, 0f, -1f), new(0f, 1f)),
            new(new(-h,  h, -h), new(0f, 0f, -1f), new(1f, 1f)),

            /// Left
            new(new(-h, -h, -h), new(-1f, 0f, 0f), new(0f, 0f)),
            new(new(-h, -h,  h), new(-1f, 0f, 0f), new(1f, 0f)),
            new(new(-h,  h,  h), new(-1f, 0f, 0f), new(1f, 1f)),
            new(new(-h,  h, -h), new(-1f, 0f, 0f), new(0f, 1f)),

            /// Right
            new(new( h, -h, -h), new(1f, 0f, 0f), new(1f, 0f)),
            new(new( h, -h,  h), new(1f, 0f, 0f), new(0f, 0f)),
            new(new( h,  h,  h), new(1f, 0f, 0f), new(0f, 1f)),
            new(new( h,  h, -h), new(1f, 0f, 0f), new(1f, 1f)),

            /// Top
            new(new(-h,  h,  h), new(0f, 1f, 0f), new(0f, 0f)),
            new(new( h,  h,  h), new(0f, 1f, 0f), new(1f, 0f)),
            new(new( h,  h, -h), new(0f, 1f, 0f), new(1f, 1f)),
            new(new(-h,  h, -h), new(0f, 1f, 0f), new(0f, 1f)),

            /// Bottom
            new(new(-h, -h,  h), new(0f, -1f, 0f), new(0f, 1f)),
            new(new( h, -h,  h), new(0f, -1f, 0f), new(1f, 1f)),
            new(new( h, -h, -h), new(0f, -1f, 0f), new(1f, 0f)),
            new(new(-h, -h, -h), new(0f, -1f, 0f), new(0f, 0f)),
        };

        uint[] indices = {
            0, 1, 2,  2, 3, 0,       /// front
            4, 6, 5,  6, 4, 7,       /// back
            8, 9, 10, 10, 11, 8,     /// left
            12, 14, 13, 14, 12, 15,  /// right
            16, 17, 18, 18, 19, 16,  /// top
            20, 22, 21, 22, 20, 23,  /// bottom
        };

        return new MeshData(vertices, indices, Silk.NET.OpenGL.PrimitiveType.Triangles);
    }


    public static MeshData GenerateWireframe (float size = 1f) {
        var half = 0.5f*size*Vector3.One;

        Span<Vector3> corners = stackalloc Vector3[8];
        corners[0] = new Vector3(-half.X, -half.Y, -half.Z);
        corners[1] = new Vector3(half.X, -half.Y, -half.Z);
        corners[2] = new Vector3(half.X, -half.Y, half.Z);
        corners[3] = new Vector3(-half.X, -half.Y, half.Z);
        corners[4] = new Vector3(-half.X, half.Y, -half.Z);
        corners[5] = new Vector3(half.X, half.Y, -half.Z);
        corners[6] = new Vector3(half.X, half.Y, half.Z);
        corners[7] = new Vector3(-half.X, half.Y, half.Z);

        var vertices = new Vertex[8];
        for (int i = 0; i < 8; i++)
            vertices[i] = new Vertex { Position = corners[i] };

        /// 12 edges, 2 indices each = 24 indices for GL_LINES.
        uint[] indices = [
            0, 1, 1, 2, 2, 3, 3, 0, /// bottom face
            4, 5, 5, 6, 6, 7, 7, 4, /// top face
            0, 4, 1, 5, 2, 6, 3, 7  /// vertical edges
        ];

        return new MeshData(vertices, indices, Silk.NET.OpenGL.PrimitiveType.Lines);
    }

}