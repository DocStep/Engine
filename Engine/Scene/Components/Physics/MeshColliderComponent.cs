using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Newtonsoft.Json;
using Engine.Graphics;

namespace Engine;


public class MeshColliderComponent : ColliderComponent {
    /// Force a convex hull even without a rigidbody.
    /// A mesh with a PhysicsComponent is always a hull, so leave this off for level geometry.
    public bool Convex = false;

    [Hide][JsonIgnore] public Graphics.Vertex[] Vertices = null!;
    [Hide][JsonIgnore] public uint[] Indices = null!;
    [Hide][JsonIgnore] Vector3 hullCenter;
    [Hide][JsonIgnore] public Graphics.Mesh? GizmoMesh { get; private set; }

    public override bool IsReady => Vertices != null && Indices != null && Indices.Length >= 3;
    public Vector3 GizmoLocalOffset => -ScaledCenter - (UseHull ? hullCenter : Vector3.Zero);

    bool UseHull => Convex || gameObject.GetComponent<PhysicsComponent>() is not null;

    /// Hull shapes are recentered around their own center, so the pose has to add it back.
    protected override RigidPose LocalPose => new RigidPose(ScaledCenter + (UseHull ? hullCenter : Vector3.Zero));


    public override void OnAdd () {
        base.OnAdd();
        MeshComponent? meshComponent = gameObject.GetComponent<MeshComponent>();
        if (meshComponent?.Mesh != null) SetMesh(meshComponent.Mesh);
    }

    public void SetMesh (Graphics.Mesh? mesh) {
        GizmoMesh?.Dispose();
        GizmoMesh = null;
        if (mesh?.Data is not Graphics.MeshData data) {
            Vertices = null!;
            Indices = null!;
            if (gameObject is not null) Refresh();
            return;
        }

        Vertices = data.Vertices;
        Indices = data.Indices;
        uint[] lineIndices = new uint[(Indices.Length/3)*6];
        for (int i = 0; i + 2 < Indices.Length; i += 3) {
            int line = i*2;
            lineIndices[line] = Indices[i];
            lineIndices[line + 1] = Indices[i + 1];
            lineIndices[line + 2] = Indices[i + 1];
            lineIndices[line + 3] = Indices[i + 2];
            lineIndices[line + 4] = Indices[i + 2];
            lineIndices[line + 5] = Indices[i];
        }
        GizmoMesh = new Graphics.Mesh(new Graphics.MeshData(Vertices, lineIndices, Silk.NET.OpenGL.PrimitiveType.Lines));
        if (gameObject != null) Refresh();
    }


    protected override TypedIndex AddShape (Shapes shapes) {
        BufferPool pool = Physics.Instance.BufferPool;
        if (UseHull) return shapes.Add(BuildHull(pool));

        int triangleCount = Indices.Length/3;
        pool.Take(triangleCount, out Buffer<Triangle> triangles);
        for (int i = 0; i < triangleCount; i++) {
            /// Reversed relative to render winding for Bepu triangle-mesh collision.
            triangles[i] = new Triangle(
                Vertices[Indices[i*3]].Position,
                Vertices[Indices[i*3 + 2]].Position,
                Vertices[Indices[i*3 + 1]].Position
            );
        }
        /// the mesh takes ownership of the triangle buffer and applies the scale itself
        return shapes.Add(new BepuPhysics.Collidables.Mesh(triangles, gameObject.Transform.Scale, pool));
    }
    public override void AddToCompound (ref CompoundBuilder builder, float weight) {
        builder.Add(BuildHull(Physics.Instance.BufferPool), LocalPose, weight);
    }
    public override void OnRemove () {
        base.OnRemove();
        GizmoMesh?.Dispose();
        GizmoMesh = null;
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