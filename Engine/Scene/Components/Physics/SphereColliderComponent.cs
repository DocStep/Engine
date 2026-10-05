using BepuPhysics;
using BepuPhysics.Collidables;

namespace Engine;


public class SphereColliderComponent : ColliderComponent {
    public float Radius = 0.5f;

    Sphere Shape {
        get {
            Vector3 s = gameObject.Transform.Scale;
            /// a sphere can't be stretched, so use the largest axis (same as Unity)
            return new Sphere(Radius*MathF.Max(MathF.Abs(s.X), MathF.Max(MathF.Abs(s.Y), MathF.Abs(s.Z))));
        }
    }

    protected override TypedIndex AddShape (Shapes shapes) {
        return shapes.Add(Shape);
    }
    public override void AddToCompound (ref CompoundBuilder builder, float weight) {
        builder.Add(Shape, LocalPose, weight);
    }
}