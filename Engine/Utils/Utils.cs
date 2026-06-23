using System;
using System.Collections.Generic;
using System.Text;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Engine;


internal class Utils {

    internal static float Clamp (float value, float min, float max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }


    internal static Matrix4X4<float> CreateFromYawPitchRoll (float yaw, float pitch, float roll) {
        return Matrix4X4.CreateRotationZ(roll)*Matrix4X4.CreateRotationX(pitch)*Matrix4X4.CreateRotationY(yaw);
    }

    internal static float Lerp (float a, float b, float t) => a + (b - a)*t;

    internal static string LoadSrc (string relativePath) {
        string fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {fullPath}.");
        return File.ReadAllText(fullPath);
    }

    internal static float[] MatrixToArray (Matrix4X4<float> m) => new[] {
        m.M11, m.M12, m.M13, m.M14,
        m.M21, m.M22, m.M23, m.M24,
        m.M31, m.M32, m.M33, m.M34,
        m.M41, m.M42, m.M43, m.M44,
    };

    /// Wraps an angle to [-pi, pi].
    internal static float WrapAngle (float angle) {
        angle %= 2f*MathF.PI;
        if (MathF.PI < angle) angle -= 2f*MathF.PI;
        if (angle < -MathF.PI) angle += 2f*MathF.PI;
        return angle;
    }

}
