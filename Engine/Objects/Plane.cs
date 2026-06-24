using System.Numerics;

namespace Engine.Graphics;


public static class Plane {

    public static MeshData Generate (float size = 1f, int divisions = 10) {
        int cells = divisions;
        int verticesPerSide = cells+1;

        var vertices = new Vertex[verticesPerSide*verticesPerSide];
        var indices = new uint[cells*cells*6];

        var half = size*0.5f;

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

        return new MeshData(vertices, indices);
    }

    public static MeshData GenerateWireframe (float size = 1f, int divisions = 10) {
        int cells = divisions;
        int verticesPerSide = cells+1;

        var vertices = new Vertex[verticesPerSide*verticesPerSide];
        var half = size*0.5f;

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

                Console.WriteLine(vertices[i].Position);
            }
        }

        var indices = new List<uint>();

        /// Horizontal lines: one full row of segments per z, (verticesPerSide-1) segments each.
        for (int z = 0; z < verticesPerSide; z++) {
            for (int x = 0; x < cells; x++) {
                uint a = (uint)(z*verticesPerSide+x);
                uint b = a+1;
                indices.Add(a);
                indices.Add(b);
            }
        }

        /// Vertical lines: one full column of segments per x, (verticesPerSide-1) segments each.
        for (int x = 0; x < verticesPerSide; x++) {
            for (int z = 0; z < cells; z++) {
                uint a = (uint)(z*verticesPerSide+x);
                uint b = (uint)((z+1)*verticesPerSide+x);
                indices.Add(a);
                indices.Add(b);
            }
        }

        return new MeshData(vertices, indices.ToArray());
    }

}