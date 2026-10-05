using BepuPhysics;
using BepuPhysics.Collidables;
using Newtonsoft.Json;

namespace Engine;


public abstract class ColliderComponent : Component {
    public Vector3 Center = Vector3.Zero;

    [Hide][JsonIgnore] public StaticHandle? StaticHandle { get; private set; }
    [Hide][JsonIgnore] TypedIndex staticShape;

    /// False while the collider has no data yet (e.g. a mesh that wasn't assigned). Skipped until ready.
    public virtual bool IsReady => true;

    /// Adds the shape to the simulation. Used only when there is no rigidbody.
    protected abstract TypedIndex AddShape (Shapes shapes);
    /// Adds the shape as a child of the rigidbody compound.
    public abstract void AddToCompound (ref CompoundBuilder builder, float weight);

    protected Vector3 ScaledCenter => Center*gameObject.Transform.Scale;
    /// Pose relative to the GameObject. Override when a shape needs an offset or rotation.
    protected virtual RigidPose LocalPose => new RigidPose(ScaledCenter);


    public override void OnAdd () {
        Refresh();
    }
    public override void OnRemove () {
        ReleaseStatic();
        if (gameObject.GetComponent<PhysicsComponent>() is PhysicsComponent rb) {
            rb.Rebuild(ignore: this);
        }
    }


    /// Call after changing shape data (radius, size, mesh, scale, ...) to rebuild the physics shape.
    public void Refresh () {
        if (gameObject.GetComponent<PhysicsComponent>() is PhysicsComponent rb) {
            rb.Rebuild();
            return;
        }
        ReleaseStatic();
        CreateStatic();
    }

    public void CreateStatic () {
        if (StaticHandle.HasValue || !IsReady) return;

        Simulation sim = Physics.Instance.Simulation;
        var t = gameObject.Transform;

        staticShape = AddShape(sim.Shapes);
        RigidPose local = LocalPose; /// read after AddShape, hulls compute their center there
        Vector3 position = t.Position + Vector3.Transform(local.Position, t.Rotation);
        Quaternion rotation = Quaternion.Concatenate(local.Orientation, t.Rotation);
        StaticHandle = sim.Statics.Add(new StaticDescription(new RigidPose(position, rotation), staticShape));
    }
    public void ReleaseStatic () {
        if (!StaticHandle.HasValue) return;

        Simulation sim = Physics.Instance.Simulation;
        sim.Statics.Remove(StaticHandle.Value);
        sim.Shapes.RecursivelyRemoveAndDispose(staticShape, Physics.Instance.BufferPool);
        StaticHandle = null;
    }

}