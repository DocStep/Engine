using System.Numerics;

namespace Engine.Graphics;


public static class Arrow3D {

    public static MeshData Generate (
        float length = 4f,
        float shaftWidth = 0.05f,
        float headLength = 1f,
        float headWidth = 0.2f,
        int radialSegments = 12) {

        float shaftLength = length - headLength;

        var verts = new List<Vertex>();
        var indices = new List<uint>();

        BuildShaft(verts, indices, shaftWidth, shaftLength, radialSegments);
        BuildCrossPlanes(verts, indices, shaftWidth, shaftLength);
        BuildHead(verts, indices, shaftLength, headWidth, headLength, radialSegments);

        MeshData data = new MeshData(verts.ToArray(), indices.ToArray(), Silk.NET.OpenGL.PrimitiveType.Triangles);
        data.RecalculateNormals();
        return data;
    }
    public static MeshData GenerateWireframe (
        float shaftLength = 4f,
        float shaftWidth = 0.05f,
        float headLength = 1f,
        float headWidth = 0.2f,
        int radialSegments = 12) {

        var verts = new List<Vertex>();
        var indices = new List<uint>();

        // Bottom ring
        uint bottomRing = (uint)verts.Count;
        AddRing(verts, shaftWidth, 0f, radialSegments);

        // Top ring (shaft/head junction)
        uint topRing = (uint)verts.Count;
        AddRing(verts, shaftWidth, shaftLength, radialSegments);

        // Head base ring
        uint headRing = (uint)verts.Count;
        AddRing(verts, headWidth, shaftLength, radialSegments);

        // Tip
        uint tipIndex = (uint)verts.Count;
        verts.Add(new Vertex { Position = new Vector3(0f, shaftLength + headLength, 0f) });

        // Bottom ring edges
        AddRingEdges(indices, bottomRing, radialSegments);
        // Top ring edges
        AddRingEdges(indices, topRing, radialSegments);
        // Head base ring edges
        AddRingEdges(indices, headRing, radialSegments);

        // Vertical shaft silhouette lines (4 evenly spaced)
        int silhouetteCount = 4;
        for (int i = 0; i < silhouetteCount; i++) {
            uint si = bottomRing + (uint)(i * radialSegments / silhouetteCount);
            uint ti = topRing  + (uint)(i * radialSegments / silhouetteCount);
            indices.Add(si); indices.Add(ti);
        }

        // Head cone spokes: head base ring → tip
        int spokeCount = 6;
        for (int i = 0; i < spokeCount; i++) {
            uint hi = headRing + (uint)(i * radialSegments / spokeCount);
            indices.Add(hi); indices.Add(tipIndex);
        }

        // Cross-plane outline (the 4 corners of each blade as a rectangle)
        AddCrossPlaneWireframe(verts, indices, shaftWidth, shaftLength, new Vector3(1f, 0f, 0f));
        AddCrossPlaneWireframe(verts, indices, shaftWidth, shaftLength, new Vector3(0f, 0f, 1f));

        return new MeshData(verts.ToArray(), indices.ToArray(), Silk.NET.OpenGL.PrimitiveType.Lines);
    }

    private static void BuildShaft (List<Vertex> verts, List<uint> indices, float radius, float length, int segments) {
        uint baseIndex = (uint)verts.Count;

        for (int i = 0; i <= segments; i++) {
            float t = i / (float)segments;
            float angle = t * MathF.PI * 2f;
            float x = MathF.Cos(angle) * radius;
            float z = MathF.Sin(angle) * radius;

            verts.Add(new Vertex { Position = new Vector3(x, 0f, z), UV = new Vector2(t, 0f) });
            verts.Add(new Vertex { Position = new Vector3(x, length, z), UV = new Vector2(t, 1f) });
        }

        for (uint i = 0; i < segments; i++) {
            uint a = baseIndex + i * 2;
            uint b = a + 1;
            uint c = baseIndex + (i + 1) * 2;
            uint d = c + 1;
            indices.Add(a); indices.Add(c); indices.Add(b);
            indices.Add(b); indices.Add(c); indices.Add(d);
        }
    }

    private static void BuildCrossPlanes (List<Vertex> verts, List<uint> indices, float halfWidth, float length) {
        AddBlade(verts, indices, halfWidth, length, new Vector3(1f, 0f, 0f));
        AddBlade(verts, indices, halfWidth, length, new Vector3(0f, 0f, 1f));
    }

    private static void AddBlade (List<Vertex> verts, List<uint> indices, float halfWidth, float length, Vector3 rightAxis) {
        uint b = (uint)verts.Count;

        Vector3 right = rightAxis * halfWidth;
        Vector3 up = new Vector3(0f, length, 0f);

        // Normal is perpendicular to the blade plane
        // rightAxis X world-up gives the face normal
        Vector3 normal = Vector3.Normalize(Vector3.Cross(rightAxis, Vector3.UnitY));

        Vector3 bl = -right;
        Vector3 br = right;
        Vector3 tl = -right + up;
        Vector3 tr = right + up;

        verts.Add(new Vertex { Position = bl, Normal =  normal, UV = new Vector2(0f, 0f) });
        verts.Add(new Vertex { Position = br, Normal =  normal, UV = new Vector2(1f, 0f) });
        verts.Add(new Vertex { Position = tl, Normal =  normal, UV = new Vector2(0f, 1f) });
        verts.Add(new Vertex { Position = tr, Normal =  normal, UV = new Vector2(1f, 1f) });

        // Back face verts get flipped normal
        verts.Add(new Vertex { Position = bl, Normal = -normal, UV = new Vector2(0f, 0f) });
        verts.Add(new Vertex { Position = br, Normal = -normal, UV = new Vector2(1f, 0f) });
        verts.Add(new Vertex { Position = tl, Normal = -normal, UV = new Vector2(0f, 1f) });
        verts.Add(new Vertex { Position = tr, Normal = -normal, UV = new Vector2(1f, 1f) });

        // Front face (verts b..b+3)
        indices.Add(b); indices.Add(b + 1); indices.Add(b + 2);
        indices.Add(b + 2); indices.Add(b + 1); indices.Add(b + 3);

        // Back face (verts b+4..b+7, same positions, flipped normal, flipped winding)
        indices.Add(b + 6); indices.Add(b + 5); indices.Add(b + 4);
        indices.Add(b + 7); indices.Add(b + 5); indices.Add(b + 6);
    }

    private static void BuildHead (List<Vertex> verts, List<uint> indices, float baseY, float radius, float headLength, int segments) {
        uint ringStart = (uint)verts.Count;
        float tipY = baseY + headLength;

        for (int i = 0; i <= segments; i++) {
            float t = i / (float)segments;
            float angle = t * MathF.PI * 2f;
            float x = MathF.Cos(angle) * radius;
            float z = MathF.Sin(angle) * radius;
            verts.Add(new Vertex { Position = new Vector3(x, baseY, z), UV = new Vector2(t, 0f) });
        }

        uint tipIndex = (uint)verts.Count;
        verts.Add(new Vertex { Position = new Vector3(0f, tipY, 0f), UV = new Vector2(0.5f, 1f) });

        for (uint i = 0; i < segments; i++) {
            indices.Add(ringStart + i);
            indices.Add(ringStart + i + 1);
            indices.Add(tipIndex);
        }

        uint capCenter = (uint)verts.Count;
        verts.Add(new Vertex { Position = new Vector3(0f, baseY, 0f), UV = new Vector2(0.5f, 0f) });

        for (uint i = 0; i < segments; i++) {
            indices.Add(capCenter);
            indices.Add(ringStart + i + 1);
            indices.Add(ringStart + i);
        }
    }
    
    
    private static void AddRing (List<Vertex> verts, float radius, float y, int segments) {
        for (int i = 0; i < segments; i++) {
            float angle = i / (float)segments * MathF.PI * 2f;
            verts.Add(new Vertex { Position = new Vector3(MathF.Cos(angle) * radius, y, MathF.Sin(angle) * radius) });
        }
    }
    private static void AddRingEdges (List<uint> indices, uint ringStart, int segments) {
        for (uint i = 0; i < segments; i++) {
            indices.Add(ringStart + i);
            indices.Add(ringStart + (i + 1) % (uint)segments);
        }
    }
    private static void AddCrossPlaneWireframe (List<Vertex> verts, List<uint> indices, float halfWidth, float length, Vector3 rightAxis) {
        uint b = (uint)verts.Count;

        Vector3 right = rightAxis * halfWidth;
        Vector3 up = new Vector3(0f, length, 0f);

        verts.Add(new Vertex { Position = -right }); // 0 bottom-left
        verts.Add(new Vertex { Position =  right }); // 1 bottom-right
        verts.Add(new Vertex { Position =  right + up }); // 2 top-right
        verts.Add(new Vertex { Position = -right + up }); // 3 top-left

        indices.Add(b); indices.Add(b + 1);
        indices.Add(b + 1); indices.Add(b + 2);
        indices.Add(b + 2); indices.Add(b + 3);
        indices.Add(b + 3); indices.Add(b);
    }


}