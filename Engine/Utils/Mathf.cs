using JQuaternion = Jitter2.LinearMath.JQuaternion;
using JVector = Jitter2.LinearMath.JVector;

namespace Engine;

public static class Mathf {

    public const float Rad2Deg = 180f/MathF.PI;
    public const float Deg2Rad = MathF.PI/180f;
    //public const float TAU = 6.2831855f;
    public const float TAU = 2*MathF.PI;


    public static Random random = new Random(25565);
    public static float R () => (float)random.NextDouble();
    public static int R (int max) => random.Next(0, max);
    public static int R (int min, int max) => random.Next(min, max);
    public static float R (float min, float max) => min + (float)random.NextDouble()*(max - min);


    public static float Clamp (float value, float min, float max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
    public static float Lerp (float a, float b, float t) => a + (b - a)*t;

    public static float Remap (float value, float startSrc, float endSrc, float startDst, float endDst) {
        return startDst + (value - startSrc)*(endDst - startDst)/(endSrc - startSrc);
    }
    public static float Remap01 (float value, float start, float end) => (value - start)/(end - start);

    public static float Saturate (float value) => value < 0 ? 0 : value;
    public static int Saturate (int value) => value < 0 ? 0 : value;
    public static float Saturate (float value, float edge) => value < edge ? edge : value;
    public static float Saturate1 (float value) => value < 1 ? 1 : value;
    public static float SaturateNegative (float value) => value < 0 ? value : 0;

    public static int Sum (params int[] values) {
        if (values == null || values.Length == 0) return 0;
        int sum = 0;
        foreach (int v in values) sum += v;
        return sum;
    }
    public static float Sum (params float[] values) {
        if (values == null || values.Length == 0) return 0f;
        float sum = 0f;
        foreach (float v in values) sum += v;
        return sum;
    }
    public static float Avg (params float[] values) {
        return values == null || values.Length == 0 ? 0 : Sum(values)/values.Length;
    }

    public static bool InRadius (float x, float y, float radius) => x*x + y*y <= radius*radius;
    public static bool InRadiusStrict (float x, float y, float radius) => x*x + y*y < radius*radius;
    public static bool InSquare (float x, float y, float radius) => -radius <= x && x <= radius && -radius <= y && y <= radius;
    public static bool InSquareStrict (float x, float y, float radius) => -radius < x && x < radius && -radius < y && y < radius;

    /// <summary> 0->0 = 1 | 0->1 = 1 | 1->0 = 0 | 1->1 = 1 </summary>
    public static bool Implies (bool a, bool b) => !a || b;

    //public static float Booly1 (this float f, bool b) => b ? f : 1;
    //public static float Booly05 (this float f, bool b) => b ? f : 0.5f;
    //public static float Booly0 (this float f, bool b) => b ? f : 0;


    public static float easeInQuad (float x) => x*x;
    public static float easeInCubic (float x) => x*x*x;
    public static float easeInSine (float x) => 1 - MathF.Cos(0.5f*(x*MathF.PI));

    public static float easeOutQuad (float x) => 1 - (1 - x)*(1 - x);
    public static float easeOutCirc (float x) => MathF.Sqrt(1f - (x - 1f)*(x - 1f));

    public static float easeInOutSine (float x) => x < 0.5f ? 4f*x*x*x : 1f - MathF.Pow(-2f*x + 2f, 3f)*0.5f;
    public static float easeInOutCubic (float x) => x < 0.5f ? 4f*x*x*x : 1f - MathF.Pow(-2f*x + 2f, 3f)*0.5f;


    public static Vector3 QuaternionToDirection (Quaternion q) {
        return new Vector3(2.0f*(q.X*q.Z + q.W*q.Y), 2.0f*(q.Y*q.Z - q.W*q.X), 1.0f - 2.0f*(q.X*q.X + q.Y*q.Y));
    }
    public static Vector3 ToEuler (Quaternion q) {
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

        return new Vector3(pitch*Rad2Deg, yaw*Rad2Deg, roll*Rad2Deg);
    }
    public static Quaternion EulerToQuaternion (Vector3 euler) {
        return Quaternion.CreateFromYawPitchRoll(euler.Y*Deg2Rad, euler.X*Deg2Rad, euler.Z*Deg2Rad);
    }
    public static Vector3 QuaternionToEuler (Quaternion q) {
        float sinr = 2f * (q.W * q.X + q.Y * q.Z);
        float cosr = 1f - 2f * (q.X * q.X + q.Y * q.Y);
        float x = MathF.Atan2(sinr, cosr);

        float sinp = 2f * (q.W * q.Y - q.Z * q.X);
        sinp = Math.Clamp(sinp, -1f, 1f);
        float y = MathF.Asin(sinp);

        float siny = 2f * (q.W * q.Z + q.X * q.Y);
        float cosy = 1f - 2f * (q.Y * q.Y + q.Z * q.Z);
        float z = MathF.Atan2(siny, cosy);

        return new Vector3(
            x * Rad2Deg,
            y * Rad2Deg,
            z * Rad2Deg
        );
    }
    public static Quaternion DirectionToQuaternion (Vector3 direction, Vector3 up = default) {
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


    extension (Matrix4x4 matrix) {
        public Quaternion GetRotation () {
            Matrix4x4.Decompose(matrix, out _, out Quaternion rotation, out _);
            return rotation;
        }
    }
    

    public static Matrix4x4 QuaternionToMatrix (Quaternion q) {
        float x = q.X, y = q.Y, z = q.Z, w = q.W;

        float xx = x * x, yy = y * y, zz = z * z;
        float xy = x * y, xz = x * z, yw = y * w;
        float yz = y * z, xw = x * w, zw = z * w;

        return new Matrix4x4(
            1f - 2f *(yy + zz), 2f *(xy + zw), 2f *(xz - yw), 0f,
            2f *(xy - zw), 1f - 2f *(xx + zz), 2f *(yz + xw), 0f,
            2f *(xz + yw), 2f *(yz - xw), 1f - 2f *(xx + yy), 0f,
            0f, 0f, 0f, 1f
        );
    }
    public static Matrix4x4 Matrix4x4FromYawPitchRoll (float yaw, float pitch, float roll) {
        return Matrix4x4.CreateRotationZ(roll)*Matrix4x4.CreateRotationX(-pitch)*Matrix4x4.CreateRotationY(-yaw);
    }

    /// Converts Euler angles (degrees) to a rotation matrix, Unity order: Y * X * Z
    extension (Vector3 euler) {
        public Matrix4x4 EulerToMatrix () {
            float x = euler.X * MathF.PI/180f;
            float y = euler.Y * MathF.PI/180f;
            float z = euler.Z * MathF.PI/180f;

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
    }
    /*extension(Matrix4x4) {
        public static Matrix4x4 EulerToMatrix1 (Vector3 euler) {
            euler *= Mathf.Deg2Rad;
            Quaternion q = Quaternion.CreateFromYawPitchRoll(euler.Y, euler.X, euler.Z);
            return Matrix4x4.CreateFromQuaternion(q);
        }
    }*/


    /// Wraps an angle to [-pi, pi].
    public static float WrapAngle (float angle) {
        angle %= 2f*MathF.PI;
        if (MathF.PI < angle) angle -= 2f*MathF.PI;
        if (angle < -MathF.PI) angle += 2f*MathF.PI;
        return angle;
    }


    //public static float Wrap (float value, float min, float max) {
    //    float range = max - min;
    //    return ((value - min)%range + range)%range + min;
    //}
    public static float Wrap (float value, float min, float max) {
    float range = max - min;
    return value - range * MathF.Floor((value - min) / range);
}
    public static Vector2 WrapVector2 (Vector2 v, float min, float max) {
        return new Vector2(Wrap(v.X, min, max), Wrap(v.Y, min, max));
    }
    public static Vector3 WrapVector3 (Vector3 v, float min, float max) {
        return new Vector3( Wrap(v.X, min, max), Wrap(v.Y, min, max), Wrap(v.Z, min, max));
    }
    public static Vector4 WrapVector4 (Vector4 v, float min, float max) {
        return new Vector4( Wrap(v.X, min, max), Wrap(v.Y, min, max), Wrap(v.Z, min, max), Wrap(v.W, min, max));
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
        public static Matrix4x4 EulerToRotationQ (float x, float y, float z) {
            Quaternion q = Quaternion.CreateFromYawPitchRoll(y*Mathf.Deg2Rad, x*Mathf.Deg2Rad, z*Mathf.Deg2Rad);
            return Matrix4x4.CreateFromQuaternion(q);
        }
    }
    extension(Matrix4x4) {
        public static Matrix4x4 DirectionToRotationM (Vector3 direction, Vector3 up = default) {
            if (up == default) up = Vector3.UnitY;
            direction = Vector3.Normalize(direction);

            if (0.999f < MathF.Abs(Vector3.Dot(direction, up))) {
                up = 0.999f < MathF.Abs(direction.Y) ? Vector3.UnitX : Vector3.UnitY;
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
