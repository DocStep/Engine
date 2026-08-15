using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Newtonsoft.Json;

namespace Engine;


public class PlaneColliderComponent : ColliderComponent, IDynamicCollider {
    [JsonIgnore] public override string Name => nameof(PlaneColliderComponent);

    [Hide][JsonIgnore] public Vector3 Position => owner.Transform.Position;
    [Hide][JsonIgnore] public Quaternion Rotation => owner.Transform.Rotation;
    [Hide][JsonIgnore] public Vector3 Scale => owner.Transform.Scale;
    [Hide][JsonIgnore] public const float thickness = 0.05f; /// thin slab, not a true zero-thickness plane

    [JsonIgnore] public TypedIndex ShapeIndex { get; private set; }


    public override void Update () { }


    public TypedIndex AddShape (Simulation simulation, BufferPool pool) {
        Box box = new Box(Scale.X, thickness, Scale.Z);
        ShapeIndex = simulation.Shapes.Add(box);
        return ShapeIndex;
    }

    public BodyInertia ComputeInertia (float mass) {
        Box box = new Box(Scale.X, thickness, Scale.Z);
        return box.ComputeInertia(mass);
    }

}
