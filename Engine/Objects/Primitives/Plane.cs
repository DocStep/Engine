namespace Engine.Graphics;


public static class Plane {

    public static MeshData GenerateQuad () {
        Vertex[] vertices = new Vertex[4] {
            new Vertex { Position = new Vector3(0, 0, 0), Normal = Vector3.UnitY, UV = new Vector2(0, 0) }, // top-left
            new Vertex { Position = new Vector3(1, 0, 0), Normal = Vector3.UnitY, UV = new Vector2(1, 0) }, // top-right
            new Vertex { Position = new Vector3(1, 0, 1), Normal = Vector3.UnitY, UV = new Vector2(1, 1) }, // bottom-right
            new Vertex { Position = new Vector3(0, 0, 1), Normal = Vector3.UnitY, UV = new Vector2(0, 1) }, // bottom-left
        };

        uint[] indices = new uint[6] { 0, 1, 2, 0, 2, 3 };

        return new MeshData(vertices, indices, Silk.NET.OpenGL.PrimitiveType.Triangles);
    }

    public static MeshData GenerateQuadUI () {
        Vertex[] vertices = new Vertex[4] {
            new Vertex { Position = new Vector3(0, 0, 0), Normal = Vector3.UnitZ, UV = new Vector2(0, 0) }, // top-left
            new Vertex { Position = new Vector3(1, 0, 0), Normal = Vector3.UnitZ, UV = new Vector2(1, 0) }, // top-right
            new Vertex { Position = new Vector3(1, 1, 0), Normal = Vector3.UnitZ, UV = new Vector2(1, 1) }, // bottom-right
            new Vertex { Position = new Vector3(0, 1, 0), Normal = Vector3.UnitZ, UV = new Vector2(0, 1) }, // bottom-left
        };

        uint[] indices = new uint[6] { 0, 1, 2, 0, 2, 3 };

        return new MeshData(vertices, indices, Silk.NET.OpenGL.PrimitiveType.Triangles);
    }

    public static MeshData Generate (float size = 1f, int divisions = 10) {
        int cells = divisions;
        int verticesPerSide = cells+1;

        Vertex[] vertices = new Vertex[verticesPerSide*verticesPerSide];
        uint[] indices = new uint[cells*cells*6];

        float half = size*0.5f;

        for (int z = 0; z < verticesPerSide; z++) {
            for (int x = 0; x < verticesPerSide; x++) {
                float u = (float)x/cells;
                float v = (float)z/cells;

                int i = z*verticesPerSide+x;
                vertices[i] = new Vertex {
                    Position = new Vector3(u*size-half, 0f, v*size-half),
                    Normal = Vector3.UnitY,
                    UV = new Vector2(u, v)
                };
            }
        }

        int idx = 0;
        for (int z = 0; z < cells; z++) {
            for (int x = 0; x < cells; x++) {
                uint topLeft = (uint)(z*verticesPerSide+x);
                uint topRight = topLeft+1;
                uint bottomLeft = (uint)((z+1)*verticesPerSide+x);
                uint bottomRight = bottomLeft+1;

                /// Counter-clockwise winding when viewed from +Y, matching Cube/Sphere.
                indices[idx++] = topLeft;
                indices[idx++] = bottomLeft;
                indices[idx++] = topRight;

                indices[idx++] = topRight;
                indices[idx++] = bottomLeft;
                indices[idx++] = bottomRight;
            }
        }

        return new MeshData(vertices, indices, Silk.NET.OpenGL.PrimitiveType.Triangles);
    }

    public static MeshData GenerateWireframe (float size = 1f, int divisions = 10) {
        int cells = divisions;
        int verticesPerSide = cells+1;

        Vertex[] vertices = new Vertex[verticesPerSide*verticesPerSide];
        float half = size*0.5f;

        for (int z = 0; z < verticesPerSide; z++) {
            for (int x = 0; x < verticesPerSide; x++) {
                float u = (float)x/cells;
                float v = (float)z/cells;

                int i = z*verticesPerSide+x;
                vertices[i] = new Vertex {
                    Position = new Vector3(u*size-half, 0f, v*size-half),
                    Normal = Vector3.UnitY,
                    UV = new Vector2(u, v)
                };
            }
        }

        List<uint> indices = new List<uint>();

        /// Each cell draws its own 4 edges independently.
        for (int z = 0; z < cells; z++) {
            for (int x = 0; x < cells; x++) {
                uint topLeft = (uint)(z*verticesPerSide+x);
                uint topRight = topLeft+1;
                uint bottomLeft = (uint)((z+1)*verticesPerSide+x);
                uint bottomRight = bottomLeft+1;

                /// Top edge
                indices.Add(topLeft);
                indices.Add(topRight);

                /// Bottom edge
                indices.Add(bottomLeft);
                indices.Add(bottomRight);

                /// Left edge
                indices.Add(topLeft);
                indices.Add(bottomLeft);

                /// Right edge
                indices.Add(topRight);
                indices.Add(bottomRight);
            }
        }

        return new MeshData(vertices, indices.ToArray(), Silk.NET.OpenGL.PrimitiveType.Lines);
    }

}