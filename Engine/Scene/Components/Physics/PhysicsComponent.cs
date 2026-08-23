using BepuPhysics;
using BepuPhysics.Collidables;
using Newtonsoft.Json;

namespace Engine;

public enum ForceMode {
    Force,
    Acceleration,
    Impulse,
    VelocityChange
}


public class PhysicsComponent : Component, IFixedUpdate {
    [JsonIgnore] public override string Name => nameof(PhysicsComponent);

    [Hide][JsonIgnore] public BodyHandle Handle { get; private set; }
    [Hide][JsonIgnore] public BodyReference Rigidbody { get; private set; }
    [Hide][JsonIgnore] TypedIndex shapeIndex;
    [JsonIgnore] float mass = 1f;
    [JsonIgnore] float friction = 1f;
    [Hide][JsonIgnore] float maximumRecoveryVelocity = 1f;
    [Hide][JsonIgnore] float frequency = 30f;
    [Hide][JsonIgnore] float dampingRation = 1f;
    [Hide][JsonIgnore] public StaticHandle? StaticHandle { get; private set; }
    [Hide][JsonIgnore] public bool isKinematicRequested = false;
    [Hide][JsonIgnore] public bool IsValid { get; private set; } = false;


    public void FixedUpdate () { }


    public override void OnAdd () {
        if (gameObject.GetComponent<ColliderComponent>() is not IDynamicCollider collider) {
            Log.log($"{gameObject.Name}: PhysicsComponent requires a dynamic-capable ColliderComponent (e.g. BoxColliderComponent).", LogType.warning);
            return;
        }

        shapeIndex = collider.AddShape(PhysicsManager.Instance.Simulation, PhysicsManager.Instance.BufferPool);
        
        BodyInertia inertia = collider.ComputeInertia(mass);
        CollidableDescription collidable = new CollidableDescription(shapeIndex, 0.1f);
        BodyDescription description = BodyDescription.CreateDynamic(
            new RigidPose(gameObject.Transform.Position, gameObject.Transform.Rotation),
            inertia,
            collidable,
            new BodyActivityDescription(0.01f)
        );

        // If kinematic/static requested, add as a static instead of a dynamic body for stability.
        if (isKinematicRequested) {
            // Shapes have already been added via collider.AddShape; reuse shapeIndex.
            StaticHandle = PhysicsManager.Instance.Simulation.Statics.Add(
                new StaticDescription(new RigidPose(gameObject.Transform.Position, gameObject.Transform.Rotation), 
                shapeIndex));
            IsValid = true;
            return;
        }

        Handle = PhysicsManager.Instance.Simulation.Bodies.Add(description);
        Rigidbody = PhysicsManager.Instance.Simulation.Bodies.GetBodyReference(Handle);
        IsValid = true;

        // If the body was created as kinematic, ensure velocities/inertia are appropriate.
        if (isKinematicRequested) {
            Rigidbody.LocalInertia = new BodyInertia();
            Rigidbody.Velocity.Linear = Vector3.Zero;
            Rigidbody.Velocity.Angular = Vector3.Zero;
        }

        PhysicsManager.Instance.BodyMaterials.Allocate(Handle) = new BodyMaterial {
            Friction = friction,
            MaximumRecoveryVelocity = maximumRecoveryVelocity,
            SpringSettings = new BepuPhysics.Constraints.SpringSettings(frequency, dampingRation)
        };


        gameObject.Transform.de_RotationChanged += SetRotation;
        gameObject.Transform.de_PositionChanged += SetPosition;
        gameObject.Transform.de_ScaleChanged += SetScale;
        gameObject.Transform.de_Stop += Stop;

        PhysicsManager.Instance.RegisterRigidbody(this);
    }
    public override void OnRemove () {
        if (StaticHandle.HasValue) {
            PhysicsManager.Instance.Simulation.Statics.Remove(StaticHandle.Value);
            StaticHandle = null;
        } else {
            PhysicsManager.Instance.RemoveRigidbody(this);
        }

        gameObject.Transform.de_RotationChanged -= SetRotation;
        gameObject.Transform.de_PositionChanged -= SetPosition;
        gameObject.Transform.de_ScaleChanged -= SetScale;
    }


    public void SetPosition (Vector3 position) {
        PhysicsManager.Instance.Simulation.Awakener.AwakenBody(Handle);
        Rigidbody.Pose.Position = position;
    }
    public void SetRotation (Quaternion rotation) {
        PhysicsManager.Instance.Simulation.Awakener.AwakenBody(Handle);
        Rigidbody.Pose.Orientation = rotation;
    }
    public void SetScale (Vector3 scale) {
        
    }

    public void UpdateTransform () {
        gameObject.Transform.SetPosition_Silent(Rigidbody.Pose.Position);
        gameObject.Transform.SetRotation_Silent(Rigidbody.Pose.Orientation);
    }
    public void UpdateRigidbody () {
        Rigidbody.Pose.Position = gameObject.Transform.Position;
        Rigidbody.Pose.Orientation = gameObject.Transform.Rotation;
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
        // Mark kinematic so OnAdd can apply it if the body isn't created yet.
        isKinematicRequested = true;
        if (!IsValid || StaticHandle.HasValue) {
            Log.log(IsValid, StaticHandle.HasValue);
            return;
        }

        Log.log("Rigidbody", Rigidbody);
        Log.log("Rigidbody.LocalInertia", Rigidbody.LocalInertia);

        try {
            Rigidbody.LocalInertia = new BodyInertia();
            Rigidbody.Velocity.Linear = Vector3.Zero;
            Rigidbody.Velocity.Angular = Vector3.Zero;
        } catch {
            /// If Rigidbody isn't created yet, OnAdd will apply the kinematic state.
        }
    }
    public void SetDynamic () {
        isKinematicRequested = false;
        if (!IsValid || StaticHandle.HasValue) return;
        try {
            Box shape = PhysicsManager.Instance.Simulation.Shapes.GetShape<Box>(shapeIndex.Index);
            Rigidbody.LocalInertia = shape.ComputeInertia(mass);
            PhysicsManager.Instance.Simulation.Awakener.AwakenBody(Handle);
        } catch {
            /// If body not yet created, inertia will be applied in OnAdd.
        }
    }

    public void AddForce (Vector3 force, ForceMode mode = ForceMode.Force) {
        if (!IsValid || StaticHandle.HasValue) return;

        PhysicsManager.Instance.Simulation.Awakener.AwakenBody(Handle);
        switch (mode) {
            case ForceMode.Force:
                /// continuous, mass-dependent -> scale by fixed timestep to get impulse
                Rigidbody.ApplyLinearImpulse((float)Time.fixedDeltaTime*force);
                break;
            case ForceMode.Acceleration:
                /// continuous, mass-independent -> scale by mass and timestep
                Rigidbody.ApplyLinearImpulse((float)Time.fixedDeltaTime*mass*force);
                break;
            case ForceMode.Impulse:
                /// instant, mass-dependent
                Rigidbody.ApplyLinearImpulse(force);
                break;
            case ForceMode.VelocityChange:
                /// instant, mass-independent -> directly modify velocity
                Rigidbody.Velocity.Linear += force;
                break;
        }
    }

}
