using System;
using System.Numerics;

namespace Engine.Graphics;


public class PhysicsComponent : Component, IComponentUpdate, IComponentFixedUpdate {

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

    RenderInfo renderInfo;


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
    public void Update () {
        renderInfo = new RenderInfo() {
            pos = owner.Transform.Position,
            rot = owner.Transform.Rotation,
            scale = 0.5f*owner.Transform.Scale,

            mesh = Renderer.Instance._mesh_GizmoSphere,
            shader = Renderer.Instance._sh_Unlit,
            material = Renderer.Instance._mat_GizmosG,
            primitiveType = Silk.NET.OpenGL.PrimitiveType.Lines,
        };
        Renderer.Instance.AddRenderInfo(renderInfo);
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
