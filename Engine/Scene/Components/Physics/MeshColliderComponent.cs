using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Newtonsoft.Json;

namespace Engine;


public class MeshColliderComponent : ColliderComponent {
    /// Force a convex hull even without a rigidbody.
    /// A mesh with a PhysicsComponent is always a hull, so leave this off for level geometry.
    public bool Convex = false;

    [Hide][JsonIgnore] public Graphics.Vertex[] Vertices = null!;
    [Hide][JsonIgnore] public uint[] Indices = null!;
    [Hide][JsonIgnore] Vector3 hullCenter;

    public override bool IsReady => Vertices != null && Indices != null && Indices.Length >= 3;

    bool UseHull => Convex || gameObject.GetComponent<PhysicsComponent>() != null;

    /// Hull shapes are recentered around their own center, so the pose has to add it back.
    protected override RigidPose LocalPose => new RigidPose(ScaledCenter + (UseHull ? hullCenter : Vector3.Zero));


    public void SetMesh (Graphics.Mesh mesh) {
        Vertices = mesh.Data.Vertices;
        Indices = mesh.Data.Indices;
        if (gameObject != null) Refresh();
    }


    protected override TypedIndex AddShape (Shapes shapes) {
        BufferPool pool = Physics.Instance.BufferPool;
        if (UseHull) return shapes.Add(BuildHull(pool));

        int triangleCount = Indices.Length/3;
        pool.Take(triangleCount, out Buffer<Triangle> triangles);
        for (int i = 0; i < triangleCount; i++) {
            triangles[i] = new Triangle(
                Vertices[Indices[i*3]].Position,
                Vertices[Indices[i*3 + 1]].Position,
                Vertices[Indices[i*3 + 2]].Position
            );
        }
        /// the mesh takes ownership of the triangle buffer and applies the scale itself
        return shapes.Add(new Mesh(triangles, gameObject.Transform.Scale, pool));
    }
    public override void AddToCompound (ref CompoundBuilder builder, float weight) {
        builder.Add(BuildHull(Physics.Instance.BufferPool), LocalPose, weight);
    }


    ConvexHull BuildHull (BufferPool pool) {
        Vector3 scale = gameObject.Transform.Scale;
        Vector3[] points = new Vector3[Vertices.Length];
        for (int i = 0; i < points.Length; i++) {
            points[i] = Vertices[i].Position*scale;
        }

        ConvexHullHelper.CreateShape(points, pool, out hullCenter, out ConvexHull hull);
        return hull;
    }
}