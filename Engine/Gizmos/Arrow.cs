using System.Numerics;
using Engine.Graphics;

namespace Engine;


public static class Arrow {
    public static MeshData Generate (
        float shaftLength = 4f,
        float shaftRadius = 0.05f,
        float headLength = 1f,
        float headRadius = 0.2f,
        int radialSegments = 12) {

        var verts = new List<Vertex>();
        var indices = new List<uint>();

        var totalLength = shaftLength + headLength;
        BuildShaft(verts, indices, shaftRadius, shaftLength, radialSegments);
        BuildHead(verts, indices, shaftLength, headRadius, headLength, radialSegments);

        var data = new MeshData(verts.ToArray(), indices.ToArray());
        data.RecalculateNormals();
        return data;
    }

    /// Open cylinder from y=0 to y=shaftLength. No caps (head cone closes the top,
    /// bottom is left open since the gizmo never needs to be seen from below/inside).
    private static void BuildShaft (List<Vertex> verts, List<uint> indices, float radius, float length, int segments) {
        uint baseIndex = (uint)verts.Count;

        for (int i = 0; i <= segments; i++) {
            float t = i/(float)segments;
            float angle = t*MathF.PI*2f;
            float x = MathF.Cos(angle)*radius;
            float z = MathF.Sin(angle)*radius;

            verts.Add(new Vertex { Position = new Vector3(x, 0f, z), UV = new Vector2(t, 0f) });
            verts.Add(new Vertex { Position = new Vector3(x, length, z), UV = new Vector2(t, 1f) });
        }

        for (uint i = 0; i < segments; i++) {
            uint a = baseIndex + i*2;
            uint b = a + 1;
            uint c = baseIndex + (i + 1)*2;
            uint d = c + 1;

            indices.Add(a); indices.Add(c); indices.Add(b);
            indices.Add(b); indices.Add(c); indices.Add(d);
        }
    }

    /// Cone from y=baseY to y=baseY+headLength, with a flat base disc so the
    /// shaft/head seam doesn't show gaps.
    private static void BuildHead (List<Vertex> verts, List<uint> indices, float baseY, float radius, float headLength, int segments) {
        uint ringStart = (uint)verts.Count;
        float tipY = baseY + headLength;

        for (int i = 0; i <= segments; i++) {
            float t = i/(float)segments;
            float angle = t*MathF.PI*2f;
            float x = MathF.Cos(angle)*radius;
            float z = MathF.Sin(angle)*radius;
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
}