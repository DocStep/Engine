using System;
using System.Numerics;

namespace Engine;


public class PhysicsComponent : Component, IComponentFixedUpdate {

    public float mass = 1f;
    public Vector3 massCenter = Vector3.Zero;
    public float drag = 0.01f;
    public float dragAngular = 0.01f;

    public bool useGravity = true;
    public bool isKinematic = false;

    /// Local-space diagonal inertia tensor (box approximation by default).
    /// Set externally once shape is known (e.g. from BoxColliderComponent on registration).
    public Vector3 inertiaDiagonal = Vector3.One;

    private static Vector3 Gravity = new Vector3(0f, -9.81f, 0f);

    private Vector3 velocity = Vector3.Zero;
    public Vector3 Velocity => getVelocity();

    private Vector3 velocityAngular = Vector3.Zero;
    public Vector3 VelocityAngular => getVelocityAngular();


    public void FixedUpdate () {
        if (isKinematic) return;
        Log.log($"rot={owner.Transform.Rotation} angVel={velocityAngular}");
        if (useGravity) {
            velocity += (float)Engine.fixedDeltaTime*Gravity;
        }

        velocity *= 1f - (float)Engine.fixedDeltaTime*drag;
        velocityAngular *= 1f - (float)Engine.fixedDeltaTime*dragAngular;
        
        const float sleepLinear = 0.01f;
        const float sleepAngular = 0.01f;
        if (velocity.LengthSquared() < sleepLinear) velocity = Vector3.Zero;
        if (velocityAngular.LengthSquared() < sleepAngular) velocityAngular = Vector3.Zero;
        
        owner.Transform.Position += (float)Engine.fixedDeltaTime*velocity;
        //owner.Transform.Rotation += (float)Engine.fixedDeltaTime*velocityAngular;
        Quaternion orientation = QuaternionFromEuler(owner.Transform.Rotation);
        Vector3 w = velocityAngular * (float)Engine.fixedDeltaTime;
        Quaternion spin = new Quaternion(w.X, w.Y, w.Z, 0f) * orientation * 0.5f;
        orientation = Quaternion.Normalize(orientation + spin);
        owner.Transform.Rotation = QuaternionToEuler(orientation);

        Log.log($"rot={owner.Transform.Rotation} angVel={velocityAngular}");
    }

    public void AddForce (Vector3 force) {
        if (isKinematic) return;
        velocity += (float)Engine.fixedDeltaTime*force/mass;
    }

    public void AddImpulse (Vector3 impulse) {
        if (isKinematic) return;
        velocity += impulse/mass;
    }
    /// Call once shape is known, e.g. from BoxColliderComponent.GetOBB() half-extents
    public void SetBoxInertia (Vector3 halfExtents) {
        float x = halfExtents.X*2f, y = halfExtents.Y*2f, z = halfExtents.Z*2f;
        inertiaDiagonal = new Vector3(
            mass*(y*y + z*z)/12f,
            mass*(x*x + z*z)/12f,
            mass*(x*x + y*y)/12f
        );
    }
    public void AddTorqueImpulse (Vector3 torque) {
        if (isKinematic) return;
        velocityAngular += new Vector3(
            torque.X/inertiaDiagonal.X,
            torque.Y/inertiaDiagonal.Y,
            torque.Z/inertiaDiagonal.Z
        );
    }

    Vector3 getVelocity () {
        return velocity;
    }
    Vector3 getVelocityAngular () {
        return velocityAngular;
    }


    /// Convert Euler angles (in radians) to Quaternion
    public static Quaternion QuaternionFromEuler (Vector3 eDeg) {
        Vector3 e = eDeg * (MathF.PI / 180f);
        return Quaternion.CreateFromYawPitchRoll(e.Y, e.X, e.Z);
    }


    public static Vector3 QuaternionToEuler (Quaternion q) {
        float sinX_cosY = 2f*(q.W*q.X - q.Y*q.Z);
        float cosX_cosY = 1f - 2f*(q.X*q.X + q.Z*q.Z);
        float x = MathF.Atan2(sinX_cosY, cosX_cosY);
        float y = MathF.Atan2(2f*(q.W*q.Y + q.X*q.Z), 1f - 2f*(q.X*q.X + q.Y*q.Y));
        float z = MathF.Atan2(2f*(q.W*q.Z + q.X*q.Y), 1f - 2f*(q.X*q.X + q.Z*q.Z));
        return new Vector3(x, y, z) * (180f / MathF.PI);
    }

}
