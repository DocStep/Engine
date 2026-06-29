using System.Numerics;

namespace Engine.Graphics;


public static class Arrow {

    public static MeshData Generate (float length = 1f, float shaftWidth = 0.05f, float headLength = 0.35f, float headWidth = 0.3f, float notchDepth = 0.12f) {
        float halfShaft = 0.5f * shaftWidth;
        float halfHead = 0.5f * headWidth;
        float shaftLength = length - headLength;

        Vertex[] vertices = {
            /// Horizontal blade (Y=0 plane)
            /// Shaft quad (0-3)
            new(new(-halfShaft, 0f, 0f),          Vector3.UnitY, new(0f, 0f)),
            new(new( halfShaft, 0f, 0f),          Vector3.UnitY, new(1f, 0f)),
            new(new( halfShaft, 0f, shaftLength), Vector3.UnitY, new(1f, 0.7f)),
            new(new(-halfShaft, 0f, shaftLength), Vector3.UnitY, new(0f, 0.7f)),
            /// Head base corners (4-5)
            new(new(-halfHead, 0f, shaftLength),  Vector3.UnitY, new(0f, 0.7f)),
            new(new( halfHead, 0f, shaftLength),  Vector3.UnitY, new(1f, 0.7f)),
            /// Tip (6), notch (7)
            new(new(0f, 0f, length),              Vector3.UnitY, new(0.5f, 1f)),
            new(new(0f, 0f, length - notchDepth), Vector3.UnitY, new(0.5f, 1f - notchDepth / headLength)),

            /// Vertical blade (X=0 plane)
            /// Shaft quad (8-11)
            new(new(0f, -halfShaft, 0f),          Vector3.UnitX, new(0f, 0f)),
            new(new(0f,  halfShaft, 0f),          Vector3.UnitX, new(1f, 0f)),
            new(new(0f,  halfShaft, shaftLength), Vector3.UnitX, new(1f, 0.7f)),
            new(new(0f, -halfShaft, shaftLength), Vector3.UnitX, new(0f, 0.7f)),
            /// Head base corners (12-13)
            new(new(0f, -halfHead, shaftLength),  Vector3.UnitX, new(0f, 0.7f)),
            new(new(0f,  halfHead, shaftLength),  Vector3.UnitX, new(1f, 0.7f)),
            /// Tip (14), notch (15)
            new(new(0f, 0f, length),              Vector3.UnitX, new(0.5f, 1f)),
            new(new(0f, 0f, length - notchDepth), Vector3.UnitX, new(0.5f, 1f - notchDepth / headLength)),
        };

        uint[] indices = {
            /// Horizontal blade
            0, 1, 2,    2, 3, 0,   /// shaft
            3, 2, 5,    5, 4, 3,   /// shoulder bridge (shaft-tip rect → head base)
            4, 7, 6,               /// left blade
            7, 5, 6,               /// right blade

            /// Vertical blade
            8,  9, 10,   10, 11, 8, /// shaft
            11, 10, 13,  13, 12, 11, /// shoulder bridge
            12, 15, 14,             /// bottom blade
            15, 13, 14,             /// top blade
        };

        return new MeshData(vertices, indices, Silk.NET.OpenGL.PrimitiveType.Triangles);
    }


    public static MeshData GenerateWireframe (float length = 1f, float shaftWidth = 0.05f, float headLength = 0.35f, float headWidth = 0.3f, float notchDepth = 0.12f) {
        float halfShaft = 0.5f * shaftWidth;
        float halfHead = 0.5f * headWidth;
        float shaftLength = length - headLength;

        Vertex[] vertices = {
            /// Horizontal blade (Y=0 plane)
            new(new(-halfShaft, 0f, 0f),          Vector3.UnitY, Vector2.Zero), /// 0  shaft base-left
            new(new( halfShaft, 0f, 0f),          Vector3.UnitY, Vector2.Zero), /// 1  shaft base-right
            new(new( halfShaft, 0f, shaftLength), Vector3.UnitY, Vector2.Zero), /// 2  shaft tip-right
            new(new(-halfShaft, 0f, shaftLength), Vector3.UnitY, Vector2.Zero), /// 3  shaft tip-left
            new(new(-halfHead,  0f, shaftLength), Vector3.UnitY, Vector2.Zero), /// 4  head base-left
            new(new( halfHead,  0f, shaftLength), Vector3.UnitY, Vector2.Zero), /// 5  head base-right
            new(new(0f, 0f, length),              Vector3.UnitY, Vector2.Zero), /// 6  tip
            new(new(0f, 0f, length - notchDepth), Vector3.UnitY, Vector2.Zero), /// 7  notch

            /// Vertical blade (X=0 plane)
            new(new(0f, -halfShaft, 0f),          Vector3.UnitX, Vector2.Zero), /// 8  shaft base-bottom
            new(new(0f,  halfShaft, 0f),          Vector3.UnitX, Vector2.Zero), /// 9  shaft base-top
            new(new(0f,  halfShaft, shaftLength), Vector3.UnitX, Vector2.Zero), /// 10 shaft tip-top
            new(new(0f, -halfShaft, shaftLength), Vector3.UnitX, Vector2.Zero), /// 11 shaft tip-bottom
            new(new(0f, -halfHead,  shaftLength), Vector3.UnitX, Vector2.Zero), /// 12 head base-bottom
            new(new(0f,  halfHead,  shaftLength), Vector3.UnitX, Vector2.Zero), /// 13 head base-top
            /// 14 = tip  → reuse index 6 (same position); but since vertices are separate arrays, duplicate:
            new(new(0f, 0f, length),              Vector3.UnitX, Vector2.Zero), /// 14 tip
            new(new(0f, 0f, length - notchDepth), Vector3.UnitX, Vector2.Zero), /// 15 notch
        };

        uint[] indices = {
            /// Horizontal
            0, 1,   /// base cap
            0, 3,   /// left shaft edge
            1, 2,   /// right shaft edge
            2, 3,   /// end cap
            4, 6,   /// left outer edge → tip
            5, 6,   /// right outer edge → tip
            4, 7,   /// left inner edge → notch
            5, 7,   /// right inner edge → notch

            /// Vertical
            8,  9,  /// base cap
            8,  11, /// bottom shaft edge
            9,  10, /// top shaft edge
            10, 11, /// end cap
            12, 14, /// bottom outer edge → tip
            13, 14, /// top outer edge → tip
            12, 15, /// bottom inner edge → notch
            13, 15, /// top inner edge → notch
        };

        return new MeshData(vertices, indices, Silk.NET.OpenGL.PrimitiveType.Lines);
    }

}