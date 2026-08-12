using BepuPhysics;
using BepuPhysics.Collidables;
using Newtonsoft.Json;
using System.Numerics;

namespace Engine;


public class PhysicsComponent : Component, IComponentFixedUpdate {
    [JsonIgnore] public override string Name => nameof(PhysicsComponent);

    [JsonIgnore] public BodyHandle Handle { get; private set; }
    [JsonIgnore] public BodyReference Rigidbody { get; private set; }
    [JsonIgnore] TypedIndex shapeIndex;
    [JsonIgnore] float mass = 1f;
    [JsonIgnore] float friction = 2f;
    [JsonIgnore] float maximumRecoveryVelocity = 1f;
    [JsonIgnore] float frequency = 300f;
    [JsonIgnore] float dampingRation = 10f;
    [JsonIgnore] public StaticHandle? StaticHandle { get; private set; }


    public void FixedUpdate () {

    }

    public override void OnAdd () {
        Vector3 scale = owner.Transform.Scale;
        Box shape = new Box(scale.X, scale.Y, scale.Z);
        shapeIndex = PhysicsManager.Instance.Simulation.Shapes.Add(shape);

        BodyDescription description = BodyDescription.CreateDynamic(
            new RigidPose(owner.Transform.Position, owner.Transform.Rotation),
            shape.ComputeInertia(mass),
            new CollidableDescription(shapeIndex, 0.1f),
            new BodyActivityDescription(0.01f)
        );

        Handle = PhysicsManager.Instance.Simulation.Bodies.Add(description);
        Rigidbody = PhysicsManager.Instance.Simulation.Bodies.GetBodyReference(Handle);

        PhysicsManager.Instance.BodyMaterials.Allocate(Handle) = new BodyMaterial {
            Friction = friction,
            MaximumRecoveryVelocity = maximumRecoveryVelocity,
            SpringSettings = new BepuPhysics.Constraints.SpringSettings(frequency, dampingRation)
        };

        PhysicsManager.Instance.RegisterRigidbody(this);
    }
    public override void OnRemove () {
        PhysicsManager.Instance.RemoveRigidbody(this);
    }


    public void SetPosition (Vector3 position) {
        Rigidbody.Pose.Position = position;
        PhysicsManager.Instance.Simulation.Awakener.AwakenBody(Handle);
    }
    public void SetRotation (Quaternion rotation) {
        Rigidbody.Pose.Orientation = rotation;
        PhysicsManager.Instance.Simulation.Awakener.AwakenBody(Handle);
    }
    public void SetScale (Vector3 scale) {
        

    }

    public void UpdateTransform () {
        owner.Transform.Position = Rigidbody.Pose.Position;
        owner.Transform.Rotation = Rigidbody.Pose.Orientation;
    }
    public void UpdateRigidbody () {
        Rigidbody.Pose.Position = owner.Transform.Position;
        Rigidbody.Pose.Orientation = owner.Transform.Rotation;
    }
    public void Stop () {
        if (Rigidbody.Kinematic) return;

        Rigidbody.Velocity.Linear = Vector3.Zero;
        Rigidbody.Velocity.Angular = Vector3.Zero;
    }

    public void SetFriction (float friction) {
        ref BodyMaterial material = ref PhysicsManager.Instance.BodyMaterials[Handle];
        material.Friction = friction;
        //PhysicsManager.Instance.BodyMaterials.Allocate(Handle) = material;
    }
    public void SetBounciness (float bounciness, bool isStaticLike = false) {
        BodyMaterial material = PhysicsManager.Instance.BodyMaterials[Handle];

        material.MaximumRecoveryVelocity = isStaticLike ? 100f : 4f;
        material.SpringSettings = isStaticLike
            ? new BepuPhysics.Constraints.SpringSettings(180, 10)
            : new BepuPhysics.Constraints.SpringSettings(30, 3); /// softer, lets torque develop

        PhysicsManager.Instance.BodyMaterials.Allocate(Handle) = material;
    }
    public void SetKinematic () {
        Rigidbody.LocalInertia = new BodyInertia();
    }
    public void SetDynamic () {
        Box shape = PhysicsManager.Instance.Simulation.Shapes.GetShape<Box>(shapeIndex.Index);
        Rigidbody.LocalInertia = shape.ComputeInertia(mass);

        PhysicsManager.Instance.Simulation.Awakener.AwakenBody(Handle);
    }

}
