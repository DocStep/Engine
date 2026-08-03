namespace Engine.Graphics;


public static class Line {

    public static MeshData GenerateWireframe (float length = 1f) {
        Vertex[] vertices = {
            new(-Vector3.UnitZ, Vector3.UnitZ, Vector2.Zero),
            new(Vector3.UnitZ, Vector3.UnitZ, Vector2.Zero),
        };

        uint[] indices = {
            0, 1, /// Z axis
        };

        return new MeshData(vertices, indices, Silk.NET.OpenGL.PrimitiveType.Lines);
    }

}