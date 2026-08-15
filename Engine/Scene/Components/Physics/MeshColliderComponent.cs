using BepuPhysics;
using BepuPhysics.Collidables;
using Newtonsoft.Json;
using BepuUtilities.Memory;

namespace Engine;


public class MeshColliderComponent : ColliderComponent {
    [JsonIgnore] public override string Name => nameof(MeshColliderComponent);

    [Hide][JsonIgnore] public Graphics.Mesh? mesh = null;

    public Vector3 Position => owner.Transform.Position;
    public Quaternion Rotation => owner.Transform.Rotation;
    public Vector3 Scale => owner.Transform.Scale;

    [JsonIgnore] float friction = 1f;
    [Hide][JsonIgnore] float maximumRecoveryVelocity = 1f;
    [Hide][JsonIgnore] float frequency = 30f;
    [Hide][JsonIgnore] float dampingRation = 1f;

    [JsonIgnore] public StaticHandle? StaticHandle { get; private set; }
    [JsonIgnore] public TypedIndex ShapeIndex { get; private set; }

    public override void OnAdd () {
        if (mesh is null) return;

        CreateCollider();
    }

    public override void OnRemove () {
        RemoveCollider();
    }

    public override void Update () { }

    private void CreateCollider () {
        if (mesh is null) return;

        RemoveCollider();

        Simulation simulation = PhysicsManager.Instance.Simulation;
        Graphics.MeshData data = mesh.Data!;

        BufferPool pool = PhysicsManager.Instance.BufferPool;
        int triangleCount = data.Indices.Length/3;
        pool.Take(triangleCount, out Buffer<Triangle> triangles);

        for (int i = 0; i < triangleCount; i++) {
            uint i0 = data.Indices[i * 3 + 0];
            uint i1 = data.Indices[i * 3 + 1];
            uint i2 = data.Indices[i * 3 + 2];

            Vector3 a = data.Vertices[i0].Position*Scale;
            Vector3 b = data.Vertices[i1].Position*Scale;
            Vector3 c = data.Vertices[i2].Position*Scale;

            triangles[i] = new Triangle(a, c, b); /// swapped b/c — flips winding if source mesh is backwards for Bepu
        }

        Mesh physicsMesh = new Mesh(triangles, new Vector3(1f), pool);

        ShapeIndex = simulation.Shapes.Add(physicsMesh);
        StaticHandle = simulation.Statics.Add(new StaticDescription(Position, Rotation, ShapeIndex));

        Log.log("MeshColliderComponent.CreateCollider", LogType.warning);
        PhysicsManager.Instance.BodyMaterials.Allocate(StaticHandle.Value) = new BodyMaterial {
            Friction = friction,
            MaximumRecoveryVelocity = maximumRecoveryVelocity,
            SpringSettings = new BepuPhysics.Constraints.SpringSettings(frequency, dampingRation)
        };
    }

    private void RemoveCollider () {
        Simulation simulation = PhysicsManager.Instance.Simulation;

        if (StaticHandle.HasValue) {
            simulation.Statics.Remove(StaticHandle.Value);
            StaticHandle = null;
        }

        if (ShapeIndex.Exists) {
            simulation.Shapes.RemoveAndDispose(ShapeIndex, PhysicsManager.Instance.BufferPool);
            ShapeIndex = default;
        }
    }

    public void SetMesh (Graphics.Mesh? mesh) {
        this.mesh = mesh;
        if (owner is not null) CreateCollider();
    }

    public void SetPosition (Vector3 position) {
        if (!StaticHandle.HasValue) return;

        PhysicsManager.Instance.Simulation.Statics[StaticHandle.Value].Pose.Position = position;
    }

    public void SetRotation (Quaternion rotation) {
        if (!StaticHandle.HasValue)
            return;

        PhysicsManager.Instance.Simulation.Statics[StaticHandle.Value].Pose.Orientation = rotation;
    }

    public void SetScale (Vector3 scale) {
        // Bepu Mesh geometry is baked when the shape is created.
        // Therefore scaling requires rebuilding the shape.
        if (mesh is not null) CreateCollider();
    }

}