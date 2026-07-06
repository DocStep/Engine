using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Newtonsoft.Json;

namespace Engine;


public class PhysicsComponent : Component, IComponentFixedUpdate {
    public PhysicsComponent () {
        Rigidbody = PhysicsManager.Instance.AddRigidbody(this);
        Rigidbody.Restitution = 0.0f;
        Rigidbody.Friction = 0.5f;
    }

    [JsonIgnore] public readonly static string typeName = typeof(PhysicsComponent).Name;

    [JsonIgnore] public readonly RigidBody Rigidbody = null!;



    public void FixedUpdate () {

    }

    public override void OnAdd () {
        Rigidbody.AddShape(new BoxShape(owner.Transform.Scale));
        UpdateRigidbody();
    }
    public override void OnRemove () {
        PhysicsManager.Instance.RemoveRigidbody(this, Rigidbody);
    }


    public void UpdateTransform () {
        owner.Transform.Position = Rigidbody.Position;
        owner.Transform.Rotation = ToEulerYXZ(Rigidbody.Orientation);
    }
    public void UpdateRigidbody () {
        Rigidbody.Position = owner.Transform.Position;
        Rigidbody.Orientation = FromEulerYXZ(owner.Transform.Rotation);
    }
    public void Stop () {
        if (Rigidbody.MotionType != MotionType.Dynamic) return;

        Rigidbody.Velocity = Vector3.Zero;
        Rigidbody.AngularVelocity = Vector3.Zero;
    }


    public static Vector3 ToEulerYXZ (JQuaternion q) {
        float x = q.X, y = q.Y, z = q.Z, w = q.W;

        /// pitch (X)
        float sinPitch = 2f*(w*x - y*z);
        sinPitch = Math.Clamp(sinPitch, -1f, 1f);
        float pitch = MathF.Asin(sinPitch);

        /// yaw (Y)
        float yaw = MathF.Atan2(2f*(w*y + x*z), 1f - 2f*(x*x + y*y));

        /// roll (Z)
        float roll = MathF.Atan2(2f*(w*z + x*y), 1f - 2f*(x*x + z*z));
        
        Vector3 eulerRadians = new Vector3(pitch, yaw, roll);
        return lib.Rad2Deg*eulerRadians;
    }
    public static JQuaternion FromEulerYXZ (Vector3 eulerDegrees) {
        Vector3 euler = eulerDegrees*lib.Deg2Rad;
        float pitch = euler.X;
        float yaw = euler.Y;
        float roll = euler.Z;

        float cy = MathF.Cos(yaw*0.5f);
        float sy = MathF.Sin(yaw*0.5f);
        float cx = MathF.Cos(pitch*0.5f);
        float sx = MathF.Sin(pitch*0.5f);
        float cz = MathF.Cos(roll*0.5f);
        float sz = MathF.Sin(roll*0.5f);

        /// q = qYaw * qPitch * qRoll, matching the YXZ composition order
        /// used in ToEulerYXZ's decomposition.
        JQuaternion q;
        q.W = cy*cx*cz + sy*sx*sz;
        q.X = cy*sx*cz + sy*cx*sz;
        q.Y = sy*cx*cz - cy*sx*sz;
        q.Z = cy*cx*sz - sy*sx*cz;

        return q;
    }

    /*public static Quaternion EulerToQuat (Vector3 euler) {
        const float d2r = MathF.PI / 180f;
        return Quaternion.CreateFromYawPitchRoll(euler.Y * d2r, euler.X * d2r, euler.Z * d2r);
    }

    public static Vector3 QuatToEuler (Quaternion q) {
        const float r2d = 180f / MathF.PI;
        // YPR → Euler XYZ (pitch/yaw/roll)
        float sinP = 2f * (q.W * q.X - q.Y * q.Z);
        float pitch = MathF.Abs(sinP) >= 1f
            ? MathF.CopySign(MathF.PI / 2f, sinP)
            : MathF.Asin(sinP);
        float yaw = MathF.Atan2(2f * (q.W * q.Y + q.Z * q.X), 1f - 2f * (q.X * q.X + q.Y * q.Y));
        float roll = MathF.Atan2(2f * (q.W * q.Z + q.X * q.Y), 1f - 2f * (q.Y * q.Y + q.Z * q.Z));
        return new Vector3(pitch * r2d, yaw * r2d, roll * r2d);
    }*/


}
