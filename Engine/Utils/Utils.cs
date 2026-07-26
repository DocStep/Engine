using JQuaternion = Jitter2.LinearMath.JQuaternion;
using JVector = Jitter2.LinearMath.JVector;

namespace Engine;

public enum Axis { XY, XZ, YZ }


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
        public static Matrix4x4 RotationEuler (float x, float y, float z) {
            Quaternion q = Quaternion.CreateFromYawPitchRoll(y*Deg2Rad, x*Deg2Rad, z*Deg2Rad);
            return Matrix4x4.CreateFromQuaternion(q);
        }
    }
    extension(Matrix4x4) {
        public static Matrix4x4 RotationEuler (Vector3 euler) {
            euler *= Deg2Rad;
            Quaternion q = Quaternion.CreateFromYawPitchRoll(euler.Y, euler.X, euler.Z);
            return Matrix4x4.CreateFromQuaternion(q);
        }
    }

    extension(Matrix4x4) {
        public static Matrix4x4 RotationFromDirection (Vector3 direction, Vector3 up = default) {
            if (up == default) up = Vector3.UnitY;
            direction = Vector3.Normalize(direction);

            if (MathF.Abs(Vector3.Dot(direction, up)) > 0.999f) {
                up = MathF.Abs(direction.Y) > 0.999f ? Vector3.UnitX : Vector3.UnitY;
            }

            Vector3 right = Vector3.Normalize(Vector3.Cross(up, direction));
            Vector3 localUp = Vector3.Cross(direction, right);

            return new Matrix4x4(
                right.X, right.Y, right.Z, 0f,
                localUp.X, localUp.Y, localUp.Z, 0f,
                direction.X, direction.Y, direction.Z, 0f,
                0f, 0f, 0f, 1f
            );
        }
    }
    public static Quaternion QRotationFromDirection (Vector3 direction, Vector3 up = default) {
        direction = Vector3.Normalize(direction);
        if (up == default) up = Vector3.UnitY;

        if (0.999f < MathF.Abs(Vector3.Dot(direction, up))) {
            up = 0.999f < MathF.Abs(direction.Y) ? Vector3.UnitX : Vector3.UnitY;
        }

        Vector3 right = Vector3.Normalize(Vector3.Cross(up, direction));
        Vector3 newUp = Vector3.Cross(direction, right);

        Matrix4x4 m = new Matrix4x4(
            right.X, right.Y, right.Z, 0f,
            newUp.X, newUp.Y, newUp.Z, 0f,
            direction.X, direction.Y, direction.Z, 0f,
            0f, 0f, 0f, 1f
        );
        return Quaternion.CreateFromRotationMatrix(m);
    }

    extension(Vector3) {
        public static Vector3 DirectionToEuler (Vector3 dir) {
            dir = Vector3.Normalize(dir);
            float yaw = MathF.Atan2(dir.X, dir.Z);
            float pitch = MathF.Asin(-dir.Y);
            return new Vector3(pitch, yaw, 0f);
        }
    }

    public static string ToString3 (this JVector vec3) {
        return $"({vec3.X:F3}, {vec3.Y:F3}, {vec3.Z:F3})";
    }


    public static JQuaternion QuaternionFromEuler (Vector3 rot) {
        Vector3 rad = rot*(MathF.PI/180f);
        JQuaternion qx = JQuaternion.CreateFromAxisAngle(JVector.UnitX, rad.X);
        JQuaternion qy = JQuaternion.CreateFromAxisAngle(JVector.UnitY, rad.Y);
        JQuaternion qz = JQuaternion.CreateFromAxisAngle(JVector.UnitZ, rad.Z);
        return qx*qy*qz;
    }





    internal static float Clamp (float value, float min, float max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }


    internal static Matrix4x4 CreateFromYawPitchRoll (float yaw, float pitch, float roll) {
        return Matrix4x4.CreateRotationZ(roll)*Matrix4x4.CreateRotationX(-pitch)*Matrix4x4.CreateRotationY(-yaw);
    }

    internal static float Lerp (float a, float b, float t) => a + (b - a)*t;

    internal static string LoadTextFile (string relativePath) {
        string fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {fullPath}.");
        return File.ReadAllText(fullPath);
    }
    extension(Matrix4x4) {
        internal static float[] ToArray (Matrix4x4 m) => new[] {
            m.M11, m.M12, m.M13, m.M14,
            m.M21, m.M22, m.M23, m.M24,
            m.M31, m.M32, m.M33, m.M34,
            m.M41, m.M42, m.M43, m.M44,
        };
    }
    extension(Silk.NET.Maths.Matrix4X4<float>) {
        public static Silk.NET.Maths.Matrix4X4<float> MatrixToMatrix (Matrix4x4 m) {
            return new Silk.NET.Maths.Matrix4X4<float>(
                m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44
            );
        }
    }
    extension(Matrix4x4) {
        internal static Matrix4x4 MatrixToMatrix (Silk.NET.Maths.Matrix4X4<float> m) {
            return new Matrix4x4(
                m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44
            );
        }
    }
    

    /*internal static Matrix4x4 CreateLookAtLH (Vector3 eye, Vector3 target, Vector3 up) {
        Vector3 zaxis = Vector3.Normalize(target - eye);        /// LH: forward = +Z, not -Z
        Vector3 xaxis = Vector3.Normalize(Vector3.Cross(up, zaxis));
        Vector3 yaxis = Vector3.Cross(zaxis, xaxis);

        return new Matrix4x4(
            xaxis.X, yaxis.X, zaxis.X, 0f,
            xaxis.Y, yaxis.Y, zaxis.Y, 0f,
            xaxis.Z, yaxis.Z, zaxis.Z, 0f,
            -Vector3.Dot(xaxis, eye), -Vector3.Dot(yaxis, eye), -Vector3.Dot(zaxis, eye), 1f
        );
    }

    internal static Matrix4x4 CreatePerspectiveFieldOfViewLH (float fov, float aspect, float near, float far) {
        float yScale = 1f/MathF.Tan(fov*0.5f);
        float xScale = yScale/aspect;
        float zRange = far/(far - near);

        return new Matrix4x4(
            xScale, 0f, 0f, 0f,
            0f, yScale, 0f, 0f,
            0f, 0f, zRange, 1f,
            0f, 0f, -near*zRange, 0f
        );
    }*/

    /// Wraps an angle to [-pi, pi].
    public static float WrapAngle (float angle) {
        angle %= 2f*MathF.PI;
        if (MathF.PI < angle) angle -= 2f*MathF.PI;
        if (angle < -MathF.PI) angle += 2f*MathF.PI;
        return angle;
    }

    public static Vector3 WrapVector3 (Vector3 v3, float min, float max) {
        float range = max - min;
        v3.X = v3.X%range;
        if (v3.X < 0f) v3.X += range;
        v3.Y = v3.Y%range;
        if (v3.Y < 0f) v3.Y += range;
        v3.Z = v3.Z%range;
        if (v3.Z < 0f) v3.Z += range;
        return v3;
    }

    public static string StringNameCapital (string text) {
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

    public static void AppendCircle (List<Graphics.Vertex> vertices, List<uint> indices, Vector3 center, float radius, int segments, Vector3 u, Vector3 v) {
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

    public static void AppendHalfCircle (List<Graphics.Vertex> vertices, List<uint> indices, Vector3 center, float radius, int segments, Vector3 u, Vector3 up, bool flip) {
        uint start = (uint)vertices.Count;
        Vector3 upDir = flip ? -up : up;
        int steps = Math.Max(segments/2, 2);

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

