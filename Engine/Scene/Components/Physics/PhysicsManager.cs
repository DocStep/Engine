using Jitter2;
using Jitter2.Dynamics;

namespace Engine;


public class PhysicsManager : Singleton<PhysicsManager> {
    public PhysicsManager () {
        World.SolverIterations = (solver: 100, relaxation: 100); /// def: 6, 4
        World.Gravity = Gravity;
        World.SubstepCount = 5; /// def: 4
    }

    public readonly World World = new World();

    public static Vector3 Gravity = new(0f, -9.81f, 0f);

    static readonly List<PhysicsComponent> PhysicsComponents = new List<PhysicsComponent>();


    public void FixedUpdate () {
        float dt = (float)Time.fixedDeltaTime;

        World.Step(dt, multiThread: true);

        int count = PhysicsComponents.Count;
        for (int i = 0; i < count; i++) {
            PhysicsComponents[i].UpdateTransform();
        }
    }


    public RigidBody AddRigidbody (PhysicsComponent physicsComponent) {
        RigidBody rigidBody = World.CreateRigidBody();
        PhysicsComponents.Add(physicsComponent);
        return rigidBody;
    }
    public void RemoveRigidbody (PhysicsComponent physicsComponent, RigidBody rigidBody) {
        PhysicsComponents.Remove(physicsComponent);
        World.Remove(rigidBody);
    }


}