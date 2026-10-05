using BepuPhysics;
using BepuPhysics.Collidables;

namespace Engine;


public class BoxColliderComponent : ColliderComponent {


    public Vector3 Size = Vector3.One;

    Box Shape {
        get {
            Vector3 s = Size*gameObject.Transform.Scale;
            return new Box(s.X, s.Y, s.Z);
        }
    }

    protected override TypedIndex AddShape (Shapes shapes) {
        return shapes.Add(Shape);
    }
    public override void AddToCompound (ref CompoundBuilder builder, float weight) {
        builder.Add(Shape, LocalPose, weight);
    }
}