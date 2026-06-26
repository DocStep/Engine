using System;
using System.Numerics;

namespace Engine;


public class PhysicsComponent : Component, IComponentUpdate {

    public float mass = 1f;
    public Vector3 massCenter = Vector3.Zero;
    public float drag = 0.01f;
    public float dragAngular = 0.01f;

    public bool useGravity = true;
    public bool isKinematic = false;

    private Vector3 velocity = Vector3.Zero;
    public Vector3 Velocity {
        get {
            return velocity;
        }
    }
    private Vector3 velocityAngular = Vector3.Zero;
    public Vector3 VelocityAngular {
        get {
            return velocityAngular;
        }
    }


    public void Update () {

    }


    public void Integrate (float dt) {
        if (isKinematic) return;

        if (useGravity)
            velocity += PhysicsManager.Gravity * dt;

        velocity *= 1f - drag*dt;
        velocityAngular *= 1f - dragAngular*dt;

        owner.Transform.Position += velocity * dt;

        // Angular integration via quaternion
        if (velocityAngular.LengthSquared() > 1e-10f) {
            float angle = velocityAngular.Length() * dt;
            Vector3 axis = velocityAngular / (angle / dt);   // = velocityAngular normalised
            var dq = Quaternion.CreateFromAxisAngle(Vector3.Normalize(velocityAngular), angle);

            // Euler rotation lives as degrees — convert, apply, convert back
            var current = EulerToQuat(owner.Transform.Rotation);
            var next = Quaternion.Normalize(dq * current);
            owner.Transform.Rotation = QuatToEuler(next);
        }
    }

    public void ApplyImpulse (Vector3 impulse, Vector3 contactOffset) {
        if (isKinematic) return;
        float invMass = 1f / mass;
        velocity        += impulse * invMass;
        velocityAngular += Vector3.Cross(contactOffset, impulse) * invMass;
        // (A real impl would multiply by inv-inertia tensor; this is the manager's job for accuracy.)
    }

    static Quaternion EulerToQuat (Vector3 euler) {
        const float d2r = MathF.PI / 180f;
        return Quaternion.CreateFromYawPitchRoll(euler.Y * d2r, euler.X * d2r, euler.Z * d2r);
    }

    static Vector3 QuatToEuler (Quaternion q) {
        const float r2d = 180f / MathF.PI;
        // YPR → Euler XYZ (pitch/yaw/roll)
        float sinP = 2f * (q.W * q.X - q.Y * q.Z);
        float pitch = MathF.Abs(sinP) >= 1f
            ? MathF.CopySign(MathF.PI / 2f, sinP)
            : MathF.Asin(sinP);
        float yaw = MathF.Atan2(2f * (q.W * q.Y + q.Z * q.X), 1f - 2f * (q.X * q.X + q.Y * q.Y));
        float roll = MathF.Atan2(2f * (q.W * q.Z + q.X * q.Y), 1f - 2f * (q.Y * q.Y + q.Z * q.Z));
        return new Vector3(pitch * r2d, yaw * r2d, roll * r2d);
    }


}
