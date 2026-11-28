using BepuPhysics;
using BepuPhysics.Collidables;

namespace Engine;


public enum ColliderAxis {
    X,
    Y,
    Z
}


public class CapsuleColliderComponent : ColliderComponent {
    public float Radius = 0.5f;
    /// Total height including both caps, like Unity.
    public float Height = 2f;
    public ColliderAxis Direction = ColliderAxis.Y;
    [Hide][Newtonsoft.Json.JsonIgnore] private Graphics.Mesh? gizmoMesh;
    [Hide][Newtonsoft.Json.JsonIgnore] private float gizmoRadius = float.NaN;
    [Hide][Newtonsoft.Json.JsonIgnore] private float gizmoLength = float.NaN;

    public Graphics.Mesh GetGizmoMesh () {
        Vector3 scale = Vector3.Abs(gameObject.Transform.Scale);
        float along = Direction switch {
            ColliderAxis.X => scale.X,
            ColliderAxis.Z => scale.Z,
            _ => scale.Y
        };
        float across = Direction switch {
            ColliderAxis.X => MathF.Max(scale.Y, scale.Z),
            ColliderAxis.Z => MathF.Max(scale.X, scale.Y),
            _ => MathF.Max(scale.X, scale.Z)
        };
        float radius = Radius*across;
        float length = MathF.Max(0f, Height*along - 2f*radius);
        if (gizmoMesh is null || gizmoRadius != radius || gizmoLength != length) {
            gizmoMesh?.Dispose();
            gizmoMesh = new Graphics.Mesh(Graphics.Capsule.GenerateWireframe(radius, length));
            gizmoRadius = radius;
            gizmoLength = length;
        }
        return gizmoMesh;
    }

    Capsule Shape {
        get {
            Vector3 s = Vector3.Abs(gameObject.Transform.Scale);
            float along, across;
            switch (Direction) {
                case ColliderAxis.X:
                    along = s.X;
                    across = MathF.Max(s.Y, s.Z);
                    break;
                case ColliderAxis.Z:
                    along = s.Z;
                    across = MathF.Max(s.X, s.Y);
                    break;
                default:
                    along = s.Y;
                    across = MathF.Max(s.X, s.Z);
                    break;
            }
            float radius = Radius*across;
            /// Bepu's length is only the straight part between the caps
            float length = MathF.Max(0f, Height*along - 2f*radius);
            return new Capsule(radius, length);
        }
    }

    /// Bepu capsules lie along Y, rotate for X/Z.
    protected override RigidPose LocalPose {
        get {
            Quaternion rotation = Direction switch {
                ColliderAxis.X => Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -MathF.PI/2f),
                ColliderAxis.Z => Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI/2f),
                _ => Quaternion.Identity
            };
            return new RigidPose(ScaledCenter, rotation);
        }
    }

    protected override TypedIndex AddShape (Shapes shapes) {
        return shapes.Add(Shape);
    }
    public override void AddToCompound (ref CompoundBuilder builder, float weight) {
        builder.Add(Shape, LocalPose, weight);
    }
    public override void OnRemove () {
        base.OnRemove();
        gizmoMesh?.Dispose();
        gizmoMesh = null;
    }
}
