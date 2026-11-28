using Engine.Graphics;

namespace Editor.Graphics;


public static class ComponentsGizmo {

    /// Call once per frame, before the render queue is built
    public static void DrawAll (Dictionary<Type, List<Component>> components) {
        if (!Constants.drawGizmos) return;

        ForEach<BoxColliderComponent>(components, c => Draw(c, Gizmos._mesh_CubeWireframe, c.GizmoDimensions));
        ForEach<CapsuleColliderComponent>(components, c => {
            if (c.Enabled) Draw(c, c.GetGizmoMesh(), Vector3.One);
        });
        ForEach<PlaneColliderComponent>(components, c => Draw(c, Gizmos._mesh_PlaneWireframe, c.GizmoDimensions,
            new Vector3(0f, c.Thickness*0.5f, 0f)));
        ForEach<SphereColliderComponent>(components, c => Draw(c, Gizmos._mesh_SphereWireframe, c.GizmoDimensions));
        ForEach<MeshColliderComponent>(components, c => DrawMesh(c));
    }

    static void ForEach<T> (Dictionary<Type, List<Component>> components, Action<T> draw) where T : Component {
        if (!components.TryGetValue(typeof(T), out List<Component>? list)) return;

        for (int i = 0; i < list.Count; i++)
            draw((T)list[i]);
    }
    static void Draw (ColliderComponent collider, Mesh mesh, Vector3 dimensions, Vector3 localOffset = default) {
        if (!collider.Enabled) return;
        Renderer.Instance.AddRenderData(new RenderData {
            model = collider.GetGizmoModel(dimensions, localOffset),
            mesh = mesh,
            material = Gizmos._mat_GizmosGreen_Instanced,
            primitiveType = Silk.NET.OpenGL.PrimitiveType.Lines,
        });
    }
    static void DrawMesh (MeshColliderComponent collider) {
        if (!collider.Enabled || collider.GizmoMesh is null) return;
        Draw(collider, collider.GizmoMesh, collider.gameObject.Transform.Scale, collider.GizmoLocalOffset);
    }

}
