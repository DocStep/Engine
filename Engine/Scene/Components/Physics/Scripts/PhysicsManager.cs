using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using System.Numerics;

namespace Engine;


public class PhysicsManager : Singleton<PhysicsManager> {
    public PhysicsManager () {
        BufferPool = new BufferPool();
        BodyMaterials = new CollidableProperty<BodyMaterial>();

        Simulation = Simulation.Create(
            BufferPool,
            new NarrowPhaseCallbacks(BodyMaterials),
            new PoseIntegratorCallbacks(Gravity),
            new SolveDescription(velocityIterationCount: 8, substepCount: 4)
        );
    }

    public readonly BufferPool BufferPool;
    public readonly Simulation Simulation;
    public readonly CollidableProperty<BodyMaterial> BodyMaterials;

    public static Vector3 Gravity = new(0f, -9.81f, 0f);

    static readonly List<PhysicsComponent> PhysicsComponents = new List<PhysicsComponent>();


    public void FixedUpdate () {
        float dt = (float)Time.fixedDeltaTime;

        Simulation.Timestep(dt);

        int count = PhysicsComponents.Count;
        for (int i = 0; i < count; i++) {
            PhysicsComponents[i].UpdateTransform();
        }
    }


    public void RegisterRigidbody (PhysicsComponent physicsComponent) {
        PhysicsComponents.Add(physicsComponent);
    }
    public void RemoveRigidbody (PhysicsComponent physicsComponent) {
        PhysicsComponents.Remove(physicsComponent);
        Simulation.Bodies.Remove(physicsComponent.Handle);
    }
}


/// Per-body material data, looked up by the narrow phase during
/// contact resolution. Mirrors Jitter2's per-RigidBody friction/restitution.
public struct BodyMaterial {
    public float Friction;
    public float MaximumRecoveryVelocity;
    public BepuPhysics.Constraints.SpringSettings SpringSettings;
}