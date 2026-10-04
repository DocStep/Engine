using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Newtonsoft.Json;

namespace Engine;


public class SphereColliderComponent : ColliderComponent, IDynamicCollider {

    [JsonIgnore] public override string Name => nameof(SphereColliderComponent);

    [Hide][JsonIgnore] public Vector3 Position = Vector3.Zero;
    public float Radius = 0.5f;

    [Hide][JsonIgnore] public TypedIndex ShapeIndex { get; private set; }


    public TypedIndex AddShape (Simulation simulation, BufferPool pool) {
        Sphere sphere = new Sphere(Radius);
        ShapeIndex = simulation.Shapes.Add(sphere);
        return ShapeIndex;
    }

    public BodyInertia ComputeInertia (float mass) {
        Sphere sphere = new Sphere(Radius);
        return sphere.ComputeInertia(mass);
    }

}
