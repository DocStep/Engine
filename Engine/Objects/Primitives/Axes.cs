namespace Engine.Graphics;


/// Debug gizmo: three lines from the origin along +X, +Y, +Z.
/// Line-list topology only — there is no meaningful solid variant, so
/// Generate and GenerateWireframe both return the same data for API symmetry
/// with Cube/Sphere/Plane/Cylinder/Capsule.
public static class Axes {

    public static MeshData GenerateWireframe () {
        Vertex[] vertices = {
            new(Vector3.Zero, Vector3.UnitX, Vector2.Zero),
            new(Vector3.UnitX, Vector3.UnitX, Vector2.Zero),

            new(Vector3.Zero, Vector3.UnitY, Vector2.Zero),
            new(Vector3.UnitY, Vector3.UnitY, Vector2.Zero),

            new(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
            new(Vector3.UnitZ, Vector3.UnitZ, Vector2.Zero),
        };

        uint[] indices = {
            0, 1, /// X axis
            2, 3, /// Y axis
            4, 5, /// Z axis
        };

        return new MeshData(vertices, indices, Silk.NET.OpenGL.PrimitiveType.Lines);
    }

}