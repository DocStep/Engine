using JQuaternion = Jitter2.LinearMath.JQuaternion;
using JVector = Jitter2.LinearMath.JVector;

namespace Engine;

public enum Axis { XY, XZ, YZ }


public static class Utils {
    

    public static string ToString3 (this JVector vec3) {
        return $"({vec3.X:F3}, {vec3.Y:F3}, {vec3.Z:F3})";
    }

    public static string NameCapital (string text) {
        return char.ToUpper(text[0]) + text.Substring(1);
    }


    public static void AppendCircle (List<Graphics.Vertex> vertices, List<uint> indices, Vector3 center, float radius, int segments, Axis axis) {
        (Vector3 u, Vector3 v) = axis switch {
            Axis.XY => (Vector3.UnitX, Vector3.UnitY),
            Axis.XZ => (Vector3.UnitX, Vector3.UnitZ),
            _ => (Vector3.UnitY, Vector3.UnitZ)
        };
        AppendCircle(vertices, indices, center, radius, segments, u, v);
    }

    public static void AppendCircle (List<Graphics.Vertex> vertices, List<uint> indices, 
        Vector3 center, float radius, int segments, Vector3 u, Vector3 v) {
        uint start = (uint)vertices.Count;

        for (int i = 0; i < segments; i++) {
            float t = i*MathF.Tau/segments;
            Vector3 pos = center+radius*(MathF.Cos(t)*u+MathF.Sin(t)*v);
            vertices.Add(new Graphics.Vertex { Position = pos });
        }

        for (int i = 0; i < segments; i++) {
            indices.Add(start+(uint)i);
            indices.Add(start+(uint)((i+1)%segments));
        }
    }

    public static void AppendHalfCircle (List<Graphics.Vertex> vertices, List<uint> indices, 
        Vector3 center, float radius, int segments, Vector3 u, Vector3 up, bool flip) {
        uint start = (uint)vertices.Count;
        Vector3 upDir = flip ? -up : up;
        int steps = (int)MathF.Max(segments/2, 2);

        for (int i = 0; i <= steps; i++) {
            float t = i*MathF.PI/steps;
            Vector3 pos = center+radius*(MathF.Cos(t)*u+MathF.Sin(t)*upDir);
            vertices.Add(new Graphics.Vertex { Position = pos });
        }

        for (int i = 0; i < steps; i++) {
            indices.Add(start+(uint)i);
            indices.Add(start+(uint)(i+1));
        }
    }


}

