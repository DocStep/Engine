namespace Engine.Graphics;


public static class Cylinder {

    public static MeshData Generate (float radius = 0.5f, float height = 1f, int segments = 24) {
        List<Vertex> vertices = new List<Vertex>();
        List<uint> indices = new List<uint>();

        float halfHeight = 0.5f*height;

        /// Side wall: two rings (top and bottom), normals point radially outward.
        int sideStart = vertices.Count;
        for (int ring = 0; ring <= 1; ring++) {
            float y = ring == 0 ? -halfHeight : halfHeight;
            float v = ring == 0 ? 0f : 1f;

            for (int seg = 0; seg <= segments; seg++) {
                float phi = 2f*MathF.PI*seg/segments;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);

                Vector3 position = new Vector3(radius*cosPhi, y, radius*sinPhi);
                Vector3 normal = new Vector3(cosPhi, 0f, sinPhi);
                Vector2 uv = new Vector2((float)seg/segments, v);

                vertices.Add(new Vertex(position, normal, uv));
            }
        }

        int sideStride = segments + 1;
        for (int seg = 0; seg < segments; seg++) {
            uint bottomLeft = (uint)(sideStart + seg);
            uint bottomRight = bottomLeft + 1;
            uint topLeft = (uint)(sideStart + sideStride + seg);
            uint topRight = topLeft + 1;

            indices.Add(bottomLeft);
            indices.Add(topLeft);
            indices.Add(bottomRight);

            indices.Add(bottomRight);
            indices.Add(topLeft);
            indices.Add(topRight);
        }

        /// Bottom cap: fan from center, normal pointing -Y.
        int bottomCenter = vertices.Count;
        vertices.Add(new Vertex(new Vector3(0f, -halfHeight, 0f), new Vector3(0f, -1f, 0f), new Vector2(0.5f, 0.5f)));

        int bottomRingStart = vertices.Count;
        for (int seg = 0; seg <= segments; seg++) {
            float phi = 2f*MathF.PI*seg/segments;
            float cosPhi = MathF.Cos(phi);
            float sinPhi = MathF.Sin(phi);

            Vector3 position = new Vector3(radius*cosPhi, -halfHeight, radius*sinPhi);
            Vector3 normal = new Vector3(0f, -1f, 0f);
            Vector2 uv = new Vector2(0.5f + 0.5f*cosPhi, 0.5f + 0.5f*sinPhi);

            vertices.Add(new Vertex(position, normal, uv));
        }

        for (int seg = 0; seg < segments; seg++) {
            uint a = (uint)(bottomRingStart + seg);
            uint b = a + 1;

            indices.Add((uint)bottomCenter);
            indices.Add(b);
            indices.Add(a);
        }

        /// Top cap: fan from center, normal pointing +Y.
        int topCenter = vertices.Count;
        vertices.Add(new Vertex(new Vector3(0f, halfHeight, 0f), new Vector3(0f, 1f, 0f), new Vector2(0.5f, 0.5f)));

        int topRingStart = vertices.Count;
        for (int seg = 0; seg <= segments; seg++) {
            float phi = 2f*MathF.PI*seg/segments;
            float cosPhi = MathF.Cos(phi);
            float sinPhi = MathF.Sin(phi);

            Vector3 position = new Vector3(radius*cosPhi, halfHeight, radius*sinPhi);
            Vector3 normal = new Vector3(0f, 1f, 0f);
            Vector2 uv = new Vector2(0.5f + 0.5f*cosPhi, 0.5f + 0.5f*sinPhi);

            vertices.Add(new Vertex(position, normal, uv));
        }

        for (int seg = 0; seg < segments; seg++) {
            uint a = (uint)(topRingStart + seg);
            uint b = a + 1;

            indices.Add((uint)topCenter);
            indices.Add(a);
            indices.Add(b);
        }

        return new MeshData(vertices.ToArray(), indices.ToArray(), Silk.NET.OpenGL.PrimitiveType.Triangles);
    }

    public static MeshData GenerateWireframe (float radius = 0.5f, float height = 1f, int segments = 24) {
        List<Vertex> vertices = new List<Vertex>();
        List<uint> indices = new List<uint>();

        float halfHeight = 0.5f*height;

        int bottomStart = vertices.Count;
        for (int seg = 0; seg < segments; seg++) {
            float phi = 2f*MathF.PI*seg/segments;
            vertices.Add(new Vertex(new Vector3(radius*MathF.Cos(phi), -halfHeight, radius*MathF.Sin(phi)), Vector3.UnitY, Vector2.Zero));
        }

        int topStart = vertices.Count;
        for (int seg = 0; seg < segments; seg++) {
            float phi = 2f*MathF.PI*seg/segments;
            vertices.Add(new Vertex(new Vector3(radius*MathF.Cos(phi), halfHeight, radius*MathF.Sin(phi)), Vector3.UnitY, Vector2.Zero));
        }

        /// Bottom and top rings.
        for (int seg = 0; seg < segments; seg++) {
            uint a = (uint)(bottomStart + seg);
            uint b = (uint)(bottomStart + (seg + 1)%segments);
            indices.Add(a);
            indices.Add(b);

            uint c = (uint)(topStart + seg);
            uint d = (uint)(topStart + (seg + 1)%segments);
            indices.Add(c);
            indices.Add(d);
        }

        /// Vertical struts, every quarter turn for readability.
        int strutStep = Math.Max(1, segments/4);
        for (int seg = 0; seg < segments; seg += strutStep) {
            indices.Add((uint)(bottomStart + seg));
            indices.Add((uint)(topStart + seg));
        }

        return new MeshData(vertices.ToArray(), indices.ToArray(), Silk.NET.OpenGL.PrimitiveType.Lines);
    }

}