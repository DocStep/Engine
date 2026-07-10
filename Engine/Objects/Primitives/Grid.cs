namespace Engine.Graphics;


public static class Grid {

    public static MeshData GenerateWireframe (float size = 10f, int divisions = 10) {
        int lines = divisions + 1;
        float half = size*0.5f;

        Vertex[] vertices = new Vertex[lines*4];
        uint[] indices = new uint[lines*4];

        int vi = 0;
        int ii = 0;

        /// Lines running along X (varying Z).
        for (int i = 0; i < lines; i++) {
            float z = (float)i/divisions*size - half;

            vertices[vi] = new Vertex(new Vector3(-half, 0f, z), Vector3.UnitY, Vector2.Zero);
            vertices[vi + 1] = new Vertex(new Vector3(half, 0f, z), Vector3.UnitY, Vector2.Zero);

            indices[ii] = (uint)vi;
            indices[ii + 1] = (uint)(vi + 1);

            vi += 2;
            ii += 2;
        }

        /// Lines running along Z (varying X).
        for (int i = 0; i < lines; i++) {
            float x = (float)i/divisions*size - half;

            vertices[vi] = new Vertex(new Vector3(x, 0f, -half), Vector3.UnitY, Vector2.Zero);
            vertices[vi + 1] = new Vertex(new Vector3(x, 0f, half), Vector3.UnitY, Vector2.Zero);

            indices[ii] = (uint)vi;
            indices[ii + 1] = (uint)(vi + 1);

            vi += 2;
            ii += 2;
        }

        return new MeshData(vertices, indices, Silk.NET.OpenGL.PrimitiveType.Lines);
    }

}