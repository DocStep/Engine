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

    extension(Matrix4x4) {
        public static Matrix4x4 RotationFromDirection (Vector3 direction, Vector3 up = default) {
            direction = Vector3.Normalize(direction);
            if (up == default) up = Vector3.UnitZ; /// reference axis must differ from the mesh's pointing axis

            if (0.999f < MathF.Abs(Vector3.Dot(direction, up))) {
                up = 0.999f < MathF.Abs(direction.Z) ? Vector3.UnitX : Vector3.UnitZ;
            }

            var right = Vector3.Normalize(Vector3.Cross(direction, up));
            var fwd = Vector3.Cross(right, direction);

            return new Matrix4x4(
                right.X, right.Y, right.Z, 0f,
                direction.X, direction.Y, direction.Z, 0f, /// Y row = mesh's pointing axis
                fwd.X, fwd.Y, fwd.Z, 0f,
                0f, 0f, 0f, 1f
            );
        }
    }
    public static Quaternion QRotationFromDirection (Vector3 direction, Vector3 up = default) {
        direction = Vector3.Normalize(direction);
        if (up == default) up = Vector3.UnitY;

        if (MathF.Abs(Vector3.Dot(direction, up)) > 0.999f) {
            up = MathF.Abs(direction.Y) > 0.999f ? Vector3.UnitX : Vector3.UnitY;
        }

        var right = Vector3.Normalize(Vector3.Cross(up, direction));
        var newUp = Vector3.Cross(direction, right);

        var m = new Matrix4x4(
            right.X, right.Y, right.Z, 0f,
            newUp.X, newUp.Y, newUp.Z, 0f,
            direction.X, direction.Y, direction.Z, 0f,
            0f, 0f, 0f, 1f
        );
        return Quaternion.CreateFromRotationMatrix(m);
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
