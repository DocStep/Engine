using System;
using System.Numerics;

namespace Engine;


internal static class Utils {

    extension(Matrix4x4) {
        public static Matrix4x4 Position (Vector3 position) {
            return Matrix4x4.CreateTranslation(position);
        }
    }
    extension(Matrix4x4) {
        public static Matrix4x4 Position (float x, float y, float z) {
            return Matrix4x4.CreateTranslation(new Vector3(x, y, z));
        }
    }

    const float Deg2Rad = MathF.PI/180f;
    extension(Matrix4x4) {
        public static Matrix4x4 Rotation (float x, float y, float z) {
            var q = Quaternion.CreateFromYawPitchRoll(y*Deg2Rad, x*Deg2Rad, z*Deg2Rad);
            return Matrix4x4.CreateFromQuaternion(q);
        }
    }
    extension(Matrix4x4) {
        public static Matrix4x4 Rotation (Vector3 euler) {
            euler *= Deg2Rad;
            var q = Quaternion.CreateFromYawPitchRoll(euler.Y, euler.X, euler.Z);
            return Matrix4x4.CreateFromQuaternion(q);
        }
    }



    internal static float Clamp (float value, float min, float max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }


    internal static Matrix4x4 CreateFromYawPitchRoll (float yaw, float pitch, float roll) {
        return Matrix4x4.CreateRotationZ(roll)*Matrix4x4.CreateRotationX(pitch)*Matrix4x4.CreateRotationY(yaw);
    }

    internal static float Lerp (float a, float b, float t) => a + (b - a)*t;

    internal static string LoadSrc (string relativePath) {
        string fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {fullPath}.");
        return File.ReadAllText(fullPath);
    }

    internal static float[] MatrixToArray (Matrix4x4 m) => new[] {
        m.M11, m.M12, m.M13, m.M14,
        m.M21, m.M22, m.M23, m.M24,
        m.M31, m.M32, m.M33, m.M34,
        m.M41, m.M42, m.M43, m.M44,
    };
    internal static Silk.NET.Maths.Matrix4X4<float> MatrixToMatrix (Matrix4x4 m) {
        return new Silk.NET.Maths.Matrix4X4<float>(
            m.M11, m.M12, m.M13, m.M14,
            m.M21, m.M22, m.M23, m.M24,
            m.M31, m.M32, m.M33, m.M34,
            m.M41, m.M42, m.M43, m.M44
        );
    }
    internal static Matrix4x4 MatrixToMatrix (Silk.NET.Maths.Matrix4X4<float> m) {
        return new Matrix4x4(
            m.M11, m.M12, m.M13, m.M14,
            m.M21, m.M22, m.M23, m.M24,
            m.M31, m.M32, m.M33, m.M34,
            m.M41, m.M42, m.M43, m.M44
        );
    }


    /// Wraps an angle to [-pi, pi].
    internal static float WrapAngle (float angle) {
        angle %= 2f*MathF.PI;
        if (MathF.PI < angle) angle -= 2f*MathF.PI;
        if (angle < -MathF.PI) angle += 2f*MathF.PI;
        return angle;
    }

}
