using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Newtonsoft.Json;

namespace Engine;

public class BoxColliderComponent : ColliderComponent, IDynamicCollider {
    [JsonIgnore] public override string Name => nameof(BoxColliderComponent);

    public Vector3 Position => owner.Transform.Position;
    public Vector3 Scale => owner.Transform.Scale;

    [JsonIgnore] public TypedIndex ShapeIndex { get; private set; }

    public override void Update () { }

    public TypedIndex AddShape (Simulation simulation, BufferPool pool) {
        Box box = new Box(Scale.X, Scale.Y, Scale.Z);
        ShapeIndex = simulation.Shapes.Add(box);
        return ShapeIndex;
    }

    public BodyInertia ComputeInertia (float mass) {
        Box box = new Box(Scale.X, Scale.Y, Scale.Z);
        return box.ComputeInertia(mass);
    }

}