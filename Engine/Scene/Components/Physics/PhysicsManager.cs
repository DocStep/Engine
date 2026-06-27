using System;
using System.Numerics;
using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace Engine;


public static class PhysicsManager {

    private readonly static World World = new World();

    public static Vector3 Gravity = new(0f, -9.81f, 0f);

    static readonly List<PhysicsComponent> PhysicsComponents = new List<PhysicsComponent>();


    public static void Init () {
        World.SolverIterations = (solver: 10, relaxation: 5); /// def: 6, 4
        World.Gravity = Gravity;
    }
    public static void FixedUpdate () {
        float dt = (float)Engine.fixedDeltaTime;

        World.Step(dt, multiThread: true);

        int count = PhysicsComponents.Count;
        for (int i = 0; i < count; i++) {
            PhysicsComponents[i].UpdateTransform();
        }
    }


    public static RigidBody AddRigidbody (PhysicsComponent physicsComponent) {
        RigidBody rigidBody = World.CreateRigidBody();
        PhysicsComponents.Add(physicsComponent);
        return rigidBody;
    }


}