using System.Linq;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using Newtonsoft.Json;

namespace Engine;

public enum ForceMode {
    Force,
    Acceleration,
    Impulse,
    VelocityChange
}


public class PhysicsComponent : Component, IFixedUpdate {

    [Hide][JsonIgnore] public BodyHandle Handle { get; private set; }
    [Hide][JsonIgnore] public BodyReference Rigidbody => PhysicsManager.Instance.Simulation.Bodies.GetBodyReference(Handle);
    [Hide][JsonIgnore] public bool IsValid { get; private set; } = false;
    [Hide][JsonIgnore] public bool isKinematicRequested = false;

    [Hide][JsonIgnore] TypedIndex shapeIndex;
    [Hide][JsonIgnore] BodyInertia dynamicInertia;
    [Hide][JsonIgnore] Vector3 centerOfMass;
    [Hide][JsonIgnore] bool isAdded = false;

    [JsonIgnore] float mass = 1f;
    [JsonIgnore] float friction = 1f;
    [Hide][JsonIgnore] float maximumRecoveryVelocity = 1f;
    [Hide][JsonIgnore] float frequency = 30f;
    [Hide][JsonIgnore] float dampingRatio = 1f;


    public void FixedUpdate () { }


    public override void OnAdd () {
        isAdded = true;
        Rebuild();

        gameObject.Transform.de_RotationChanged += SetRotation;
        gameObject.Transform.de_PositionChanged += SetPosition;
        gameObject.Transform.de_ScaleChanged += SetScale;
        gameObject.Transform.de_Stop += Stop;

        PhysicsManager.Instance.RegisterRigidbody(this);
    }
    public override void OnRemove () {
        gameObject.Transform.de_RotationChanged -= SetRotation;
        gameObject.Transform.de_PositionChanged -= SetPosition;
        gameObject.Transform.de_ScaleChanged -= SetScale;
        gameObject.Transform.de_Stop -= Stop;

        PhysicsManager.Instance.UnregisterRigidbody(this);
        DestroyBody();
        isAdded = false;

        /// colliders go back to being standalone statics
        foreach (ColliderComponent collider in gameObject.GetComponents<ColliderComponent>()) {
            collider.CreateStatic();
        }
    }


    /// Builds one body from every collider on this object.
    /// Called when a collider is added/removed or when mass/scale changes.
    public void Rebuild (ColliderComponent? ignore = null) {
        if (!isAdded) return;

        Simulation sim = PhysicsManager.Instance.Simulation;
        BufferPool pool = PhysicsManager.Instance.BufferPool;

        Vector3 linear = Vector3.Zero;
        Vector3 angular = Vector3.Zero;
        if (IsValid) {
            linear = Rigidbody.Velocity.Linear;
            angular = Rigidbody.Velocity.Angular;
            DestroyBody();
        }

        List<ColliderComponent> colliders = gameObject.GetComponents<ColliderComponent>().Where(c => c != ignore && c.IsReady).ToList();
        if (colliders.Count == 0) {
            Log.log($"{gameObject.Name}: PhysicsComponent needs at least one ColliderComponent.", LogType.warning);
            return;
        }

        CompoundBuilder builder = new CompoundBuilder(pool, sim.Shapes, colliders.Count);
        float weight = mass / colliders.Count;
        foreach (ColliderComponent collider in colliders) {
            collider.ReleaseStatic();
            collider.AddToCompound(ref builder, weight);
        }
        builder.BuildDynamicCompound(out Buffer<CompoundChild> children, out BodyInertia inertia, out Vector3 center);
        builder.Dispose();

        centerOfMass = center;
        dynamicInertia = inertia;
        shapeIndex = sim.Shapes.Add(new Compound(children));

        var t = gameObject.Transform;
        BodyDescription description = BodyDescription.CreateDynamic(
            new RigidPose(t.Position + Vector3.Transform(centerOfMass, t.Rotation), t.Rotation),
            isKinematicRequested ? new BodyInertia() : dynamicInertia,
            new CollidableDescription(shapeIndex, 0.1f),
            new BodyActivityDescription(0.01f)
        );

        Handle = sim.Bodies.Add(description);
        Rigidbody.Velocity.Linear = linear;
        Rigidbody.Velocity.Angular = angular;
        ApplyMaterial();
        IsValid = true;
    }
    void DestroyBody () {
        if (!IsValid) return;

        Simulation sim = PhysicsManager.Instance.Simulation;
        sim.Bodies.Remove(Handle);
        sim.Shapes.RecursivelyRemoveAndDispose(shapeIndex, PhysicsManager.Instance.BufferPool);
        IsValid = false;
    }
    void ApplyMaterial () {
        PhysicsManager.Instance.BodyMaterials.Allocate(Handle) = new BodyMaterial {
            Friction = friction,
            MaximumRecoveryVelocity = maximumRecoveryVelocity,
            SpringSettings = new SpringSettings(frequency, dampingRatio)
        };
    }


    /// Body pose = transform pose shifted to the compound's center of mass.
    void ApplyPose (Vector3 position, Quaternion rotation) {
        if (!IsValid) return;

        PhysicsManager.Instance.Simulation.Awakener.AwakenBody(Handle);
        Rigidbody.Pose.Position = position + Vector3.Transform(centerOfMass, rotation);
        Rigidbody.Pose.Orientation = rotation;
    }
    public void SetPosition (Vector3 position) {
        ApplyPose(position, gameObject.Transform.Rotation);
    }
    public void SetRotation (Quaternion rotation) {
        ApplyPose(gameObject.Transform.Position, rotation);
    }
    public void SetScale (Vector3 scale) {
        Rebuild();
    }

    public void UpdateTransform () {
        Quaternion rotation = Rigidbody.Pose.Orientation;
        gameObject.Transform.SetPosition_Silent(Rigidbody.Pose.Position - Vector3.Transform(centerOfMass, rotation));
        gameObject.Transform.SetRotation_Silent(rotation);
    }
    public void UpdateRigidbody () {
        ApplyPose(gameObject.Transform.Position, gameObject.Transform.Rotation);
    }
    public void Stop () {
        if (!IsValid || Rigidbody.Kinematic) return;

        Rigidbody.Velocity.Linear = Vector3.Zero;
        Rigidbody.Velocity.Angular = Vector3.Zero;
    }


    public void SetMass (float mass) {
        this.mass = mass;
        Rebuild();
    }
    public void SetFriction (float friction) {
        this.friction = friction;
        if (IsValid) ApplyMaterial();
    }
    public void SetBounciness (float bounciness, bool isStaticLike = false) {
        maximumRecoveryVelocity = isStaticLike ? 100f : 4f;
        frequency = isStaticLike ? 180f : 30f;
        dampingRatio = isStaticLike ? 10f : 3f; /// softer, lets torque develop
        if (IsValid) ApplyMaterial();
    }
    public void SetKinematic () {
        isKinematicRequested = true;
        if (!IsValid) return;

        Rigidbody.LocalInertia = new BodyInertia();
        Rigidbody.Velocity.Linear = Vector3.Zero;
        Rigidbody.Velocity.Angular = Vector3.Zero;
    }
    public void SetDynamic () {
        isKinematicRequested = false;
        if (!IsValid) return;

        Rigidbody.LocalInertia = dynamicInertia;
        PhysicsManager.Instance.Simulation.Awakener.AwakenBody(Handle);
    }

    public void AddForce (Vector3 force, ForceMode mode = ForceMode.Force) {
        if (!IsValid || isKinematicRequested) return;

        PhysicsManager.Instance.Simulation.Awakener.AwakenBody(Handle);
        switch (mode) {
            case ForceMode.Force:
                /// continuous, mass-dependent -> force*dt = impulse
                Rigidbody.ApplyLinearImpulse((float)Time.fixedDeltaTime*force);
                break;
            case ForceMode.Acceleration:
                /// continuous, mass-independent -> mass*acceleration*dt = impulse
                Rigidbody.ApplyLinearImpulse((float)Time.fixedDeltaTime*mass*force);
                break;
            case ForceMode.Impulse:
                /// instant, mass-dependent
                Rigidbody.ApplyLinearImpulse(force);
                break;
            case ForceMode.VelocityChange:
                /// instant, mass-independent
                Rigidbody.Velocity.Linear += force;
                break;
        }
    }
}