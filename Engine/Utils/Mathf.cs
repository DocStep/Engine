using JQuaternion = Jitter2.LinearMath.JQuaternion;
using JVector = Jitter2.LinearMath.JVector;

namespace Engine;

public static class Mathf {



    public const float Rad2Deg = 180f/MathF.PI;
    public const float Deg2Rad = MathF.PI/180f;


    public static Vector3 QuaternionToEuler (Quaternion q) {
        Matrix4x4 m = Matrix4x4.CreateFromQuaternion(q);

        float pitch = MathF.Asin(-m.M23);

        float yaw;
        float roll;

        if (MathF.Abs(m.M23) < 0.999999f) {
            yaw = MathF.Atan2(m.M13, m.M33);
            roll = MathF.Atan2(m.M21, m.M22);
        } else {
            yaw = MathF.Atan2(-m.M31, m.M11);
            roll = 0;
        }

        return new Vector3(
            pitch*Rad2Deg,
            yaw*Rad2Deg,
            roll*Rad2Deg);
    }
    public static Quaternion DirectionToQRotation (Vector3 direction, Vector3 up = default) {
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

    public static Quaternion EulerToQuaternion (Vector3 euler) {
        return Quaternion.CreateFromYawPitchRoll(
            euler.Y*Deg2Rad,
            euler.X*Deg2Rad,
            euler.Z*Deg2Rad);
    }


    public static JQuaternion EulerToJQuaternion (Vector3 rot) {
        Vector3 rad = rot*(MathF.PI/180f);
        JQuaternion qx = JQuaternion.CreateFromAxisAngle(JVector.UnitX, rad.X);
        JQuaternion qy = JQuaternion.CreateFromAxisAngle(JVector.UnitY, rad.Y);
        JQuaternion qz = JQuaternion.CreateFromAxisAngle(JVector.UnitZ, rad.Z);
        return qx*qy*qz;
    }





    public static float Clamp (float value, float min, float max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }


    public static Matrix4x4 Matrix4x4FromYawPitchRoll (float yaw, float pitch, float roll) {
        return Matrix4x4.CreateRotationZ(roll)*Matrix4x4.CreateRotationX(-pitch)*Matrix4x4.CreateRotationY(-yaw);
    }

    /// Converts Euler angles (degrees) to a rotation matrix, Unity order: Y * X * Z
    public static Matrix4x4 EulerToMatrix (Vector3 eulerDegrees) {
        float x = eulerDegrees.X * MathF.PI/180f;
        float y = eulerDegrees.Y * MathF.PI/180f;
        float z = eulerDegrees.Z * MathF.PI/180f;

        float sx = MathF.Sin(x), cx = MathF.Cos(x);
        float sy = MathF.Sin(y), cy = MathF.Cos(y);
        float sz = MathF.Sin(z), cz = MathF.Cos(z);

        /// Row-vector rotation matrices (LH), each transforms v' = v*M
        var rx = new Matrix4x4(
            1, 0, 0, 0,
            0, cx, sx, 0,
            0, -sx, cx, 0,
            0, 0, 0, 1);

        var ry = new Matrix4x4(
            cy, 0, -sy, 0,
            0, 1, 0, 0,
            sy, 0, cy, 0,
            0, 0, 0, 1);

        var rz = new Matrix4x4(
            cz, sz, 0, 0,
            -sz, cz, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1);

        /// Combined: yaw * pitch * roll, applied in that order to a row vector
        return rz * rx * ry;
    }

    public static float Lerp (float a, float b, float t) => a + (b - a)*t;
    

    /// Wraps an angle to [-pi, pi].
    public static float WrapAngle (float angle) {
        angle %= 2f*MathF.PI;
        if (MathF.PI < angle) angle -= 2f*MathF.PI;
        if (angle < -MathF.PI) angle += 2f*MathF.PI;
        return angle;
    }


    public static float Wrap (float value, float min, float max) {
        float range = max - min;
        return ((value - min)%range + range)%range + min;
    }
    public static Vector2 WrapVector2 (Vector2 v, float min, float max) {
        return new Vector2(
            Wrap(v.X, min, max),
            Wrap(v.Y, min, max));
    }
    public static Vector3 WrapVector3 (Vector3 v, float min, float max) {
        return new Vector3(
            Wrap(v.X, min, max),
            Wrap(v.Y, min, max),
            Wrap(v.Z, min, max));
    }
    public static Vector4 WrapVector4 (Vector4 v, float min, float max) {
        return new Vector4(
            Wrap(v.X, min, max),
            Wrap(v.Y, min, max),
            Wrap(v.Z, min, max),
            Wrap(v.W, min, max));
    }


    extension(Matrix4x4) {
        public static float[] ToArray (Matrix4x4 m) => new[] {
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
        public static Matrix4x4 MatrixToMatrix (Silk.NET.Maths.Matrix4X4<float> m) {
            return new Matrix4x4(
                m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44
            );
        }
    }




    extension(Matrix4x4) {
        public static Matrix4x4 Position (float x, float y, float z) {
            return Matrix4x4.CreateTranslation(new Vector3(x, y, z));
        }
    }

    extension(Matrix4x4) {
        public static Matrix4x4 RotationEuler (float x, float y, float z) {
            Quaternion q = Quaternion.CreateFromYawPitchRoll(y*Mathf.Deg2Rad, x*Mathf.Deg2Rad, z*Mathf.Deg2Rad);
            return Matrix4x4.CreateFromQuaternion(q);
        }
    }
    extension(Matrix4x4) {
        public static Matrix4x4 RotationEuler (Vector3 euler) {
            euler *= Mathf.Deg2Rad;
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

    extension(Vector3) {
        public static Vector3 DirectionToEuler (Vector3 dir) {
            dir = Vector3.Normalize(dir);
            float pitch = MathF.Asin(-Mathf.Clamp(dir.Y, -1f, 1f))*Mathf.Rad2Deg;
            float yaw = MathF.Atan2(dir.X, dir.Z)*Mathf.Rad2Deg;
            return new Vector3(pitch, yaw, 0f);
        }
    }

}
