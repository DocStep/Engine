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

    private static readonly Vector3 Gravity = new Vector3(0f, -9.81f, 0f);

    private Vector3 velocity = Vector3.Zero;
    public Vector3 Velocity => getVelocity();

    private Vector3 velocityAngular = Vector3.Zero;
    public Vector3 VelocityAngular => getVelocityAngular();


    public void FixedUpdate () {
        if (isKinematic) return;

        if (useGravity) {
            velocity += (float)Engine.fixedDeltaTime*Gravity;
        }

        velocity *= 1f - (float)Engine.fixedDeltaTime*drag;
        velocityAngular *= 1f - (float)Engine.fixedDeltaTime*dragAngular;

        owner.Transform.Position += (float)Engine.fixedDeltaTime*velocity;
        owner.Transform.Rotation += (float)Engine.fixedDeltaTime*velocityAngular;
    }

    public void AddForce (Vector3 force) {
        if (isKinematic) return;
        velocity += (float)Engine.fixedDeltaTime*force/mass;
    }

    public void AddImpulse (Vector3 impulse) {
        if (isKinematic) return;
        velocity += impulse/mass;
    }

    Vector3 getVelocity () {
        return velocity;
    }
    Vector3 getVelocityAngular () {
        return velocityAngular;
    }

}
