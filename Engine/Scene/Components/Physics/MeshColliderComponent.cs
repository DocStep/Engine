using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Newtonsoft.Json;

namespace Engine;


public class MeshColliderComponent : ColliderComponent {
    /// Convex = one convex hull around the mesh, works on a rigidbody.
    /// Not convex = exact triangles, static only (same limit as Unity).
    public bool Convex = false;

    [Hide][JsonIgnore] public Graphics.Vertex[] Vertices = null!;
    [Hide][JsonIgnore] public uint[] Indices = null!;
    [Hide][JsonIgnore] Vector3 hullCenter;

    public override bool IsReady => Vertices != null && Indices != null && Indices.Length >= 3;

    /// Hull shapes are recentered around their own center, so the pose has to add it back.
    protected override RigidPose LocalPose => new RigidPose(ScaledCenter + (Convex ? hullCenter : Vector3.Zero));


    public void SetMesh (Graphics.Mesh mesh) {
        if (mesh is null || mesh.Data is null) return;

        Vertices = mesh.Data.Vertices;
        Indices = mesh.Data.Indices;
        if (gameObject != null) Refresh();
    }


    protected override TypedIndex AddShape (Shapes shapes) {
        BufferPool pool = PhysicsManager.Instance.BufferPool;
        if (Convex) return shapes.Add(BuildHull(pool));

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
        if (!Convex) {
            Log.log($"{gameObject.Name}: non-convex MeshCollider can't be used with a PhysicsComponent. Enable Convex.", LogType.warning);
            return;
        }
        builder.Add(BuildHull(PhysicsManager.Instance.BufferPool), LocalPose, weight);
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