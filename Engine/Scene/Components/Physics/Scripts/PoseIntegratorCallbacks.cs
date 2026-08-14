using BepuPhysics;
using BepuUtilities;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Engine;


public struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks {
    public PoseIntegratorCallbacks (Vector3 gravity) {
        Gravity = gravity;
        gravityWideDt = default;
    }

    public Vector3 Gravity;
    Vector3Wide gravityWideDt;

    public readonly AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
    public readonly bool AllowSubstepsForUnconstrainedBodies => false;
    public readonly bool IntegrateVelocityForKinematics => false;

    public void Initialize (Simulation simulation) { }

    public void PrepareForIntegration (float dt) {
        gravityWideDt = Vector3Wide.Broadcast(Gravity*dt);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IntegrateVelocity (Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity) {
        velocity.Linear += gravityWideDt;
    }
}
