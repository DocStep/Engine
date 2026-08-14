using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;

namespace Engine;


public struct NarrowPhaseCallbacks : INarrowPhaseCallbacks {
    public NarrowPhaseCallbacks (CollidableProperty<BodyMaterial> bodyMaterials) {
        BodyMaterials = bodyMaterials;
    }

    public CollidableProperty<BodyMaterial> BodyMaterials;

    public void Initialize (Simulation simulation) {
        BodyMaterials.Initialize(simulation);
    }

    public bool AllowContactGeneration (int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin) => true;

    public bool ConfigureContactManifold<TManifold> (int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold> {
        var materialA = a_IsBody(pair.A) ? BodyMaterials[pair.A.BodyHandle] : default;
        var materialB = a_IsBody(pair.B) ? BodyMaterials[pair.B.BodyHandle] : default;

        pairMaterial.FrictionCoefficient = System.MathF.Min(materialA.Friction, materialB.Friction);
        pairMaterial.MaximumRecoveryVelocity = System.MathF.Max(materialA.MaximumRecoveryVelocity, materialB.MaximumRecoveryVelocity);
        pairMaterial.SpringSettings = materialA.SpringSettings;
        return true;
    }

    static bool a_IsBody (CollidableReference c) => c.Mobility != CollidableMobility.Static;

    public bool AllowContactGeneration (int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) => true;
    public bool ConfigureContactManifold (int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold) => true;

    public void Dispose () { }
}
