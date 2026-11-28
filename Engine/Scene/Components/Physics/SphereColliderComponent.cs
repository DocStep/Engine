using BepuPhysics;
using BepuPhysics.Collidables;

namespace Engine;


public class SphereColliderComponent : ColliderComponent {
    public float Radius = 0.5f;
    public Vector3 GizmoDimensions {
        get {
            float diameter = 2f*Radius*MathF.Max(MathF.Abs(gameObject.Transform.Scale.X),
                MathF.Max(MathF.Abs(gameObject.Transform.Scale.Y), MathF.Abs(gameObject.Transform.Scale.Z)));
            return new Vector3(diameter);
        }
    }

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