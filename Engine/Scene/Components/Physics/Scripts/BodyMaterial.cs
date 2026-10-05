namespace Engine;


/// Per-body material data, looked up by the narrow phase during contact resolution.
public struct BodyMaterial {

    public float Friction;
    public float MaximumRecoveryVelocity;
    public BepuPhysics.Constraints.SpringSettings SpringSettings;

}
