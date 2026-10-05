using BepuPhysics;
using BepuPhysics.Collidables;

namespace Engine;


public class PlaneColliderComponent : ColliderComponent {
    /// Width (X) and length (Z).
    public Vector2 Size = new Vector2(100f, 100f);
    /// Thick enough that fast objects can't tunnel through.
    public float Thickness = 1f;

    Box Shape {
        get {
            Vector3 s = Vector3.Abs(gameObject.Transform.Scale);
            return new Box(Size.X*s.X, Thickness, Size.Y*s.Z);
        }
    }

    /// Push the box down so its top face is the plane.
    protected override RigidPose LocalPose => new RigidPose(ScaledCenter - new Vector3(0f, Thickness*0.5f, 0f));

    protected override TypedIndex AddShape (Shapes shapes) {
        return shapes.Add(Shape);
    }
    public override void AddToCompound (ref CompoundBuilder builder, float weight) {
        builder.Add(Shape, LocalPose, weight);
    }
}