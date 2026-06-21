using Silk.NET.Maths;

namespace Engine.Graphics;


/// Generates a unit cube as MeshData. No GL here — wrap the result in a
/// Mesh to actually draw it: new Mesh(gl, Cube.Generate()).
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

        return new MeshData(vertices, indices);
    }
}