using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Newtonsoft.Json;

namespace Engine;


public class CapsuleColliderComponent : ColliderComponent, IDynamicCollider {

    [JsonIgnore] public override string Name => nameof(CapsuleColliderComponent);

    public Vector3 Position = Vector3.Zero;
    public float Height = 1f;
    public float Radius = 0.5f;

    [Hide][JsonIgnore] public TypedIndex ShapeIndex { get; private set; }


    public TypedIndex AddShape (Simulation simulation, BufferPool pool) {
        Capsule capsule = new Capsule(Radius, Height * 0.5f);
        ShapeIndex = simulation.Shapes.Add(capsule);
        return ShapeIndex;
    }

    public BodyInertia ComputeInertia (float mass) {
        Capsule capsule = new Capsule(Radius, Height * 0.5f);
        return capsule.ComputeInertia(mass);
    }

}
