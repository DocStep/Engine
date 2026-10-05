using BepuPhysics;
using BepuUtilities.Memory;
using Newtonsoft.Json;

namespace Engine;


public class Physics : Singleton<Physics> {
    public Physics () {
        BufferPool = new BufferPool();
        BodyMaterials = new CollidableProperty<BodyMaterial>();

        Simulation = Simulation.Create(
            BufferPool,
            new NarrowPhaseCallbacks(BodyMaterials),
            new PoseIntegratorCallbacks(), /// reads Physics.Gravity every step
            new SolveDescription(velocityIterationCount: 8, substepCount: 4)
        );
    }

    [Hide] private static Vector3 gravity = new(0f, -9.81f, 0f);
    public static Vector3 Gravity {
        get => gravity;
        set {
            gravity = value;
            for (int i = 0; i < Instance.PhysicsComponents.Count; i++) {
                if (Instance.PhysicsComponents[i].IsValid)
                    Instance.Simulation.Awakener.AwakenBody(Instance.PhysicsComponents[i].Handle);
            }
        }
    }
    [Hide] public static float VelocitySleepThreshold = 0.001f;
    [Hide] public static float maximumSpeculativeMargin = 0.1f;

    private readonly List<PhysicsComponent> PhysicsComponents = new List<PhysicsComponent>();

    [Hide, JsonIgnore] public readonly BufferPool BufferPool;
    [Hide, JsonIgnore] public readonly Simulation Simulation;
    [Hide, JsonIgnore] public readonly CollidableProperty<BodyMaterial> BodyMaterials;


    public void RegisterRigidbody (PhysicsComponent physicsComponent) {
        PhysicsComponents.Add(physicsComponent);
    }
    public void UnregisterRigidbody (PhysicsComponent physicsComponent) {
        PhysicsComponents.Remove(physicsComponent);
    }


    public void FixedUpdate () {
        float dt = (float)Time.fixedDeltaTime;

        Simulation.Timestep(dt);

        int count = PhysicsComponents.Count;
        for (int i = 0; i < count; i++) {
            if (PhysicsComponents[i].IsValid)
                PhysicsComponents[i].UpdateTransform();
        }
    }

}