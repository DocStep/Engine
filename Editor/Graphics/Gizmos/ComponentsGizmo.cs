using Engine.Graphics;

namespace Editor.Graphics;


public static class ComponentsGizmo {

    /// Call once per frame, before the render queue is built
    public static void DrawAll (Dictionary<Type, List<Component>> components) {
        if (!Constants.drawGizmos) return;

        ForEach<BoxColliderComponent>(components, c => c.DrawGizmo());
        ForEach<CapsuleColliderComponent>(components, c => c.DrawGizmo());
        ForEach<PlaneColliderComponent>(components, c => c.DrawGizmo());
        ForEach<SphereColliderComponent>(components, c => c.DrawGizmo());
    }

    static void ForEach<T> (Dictionary<Type, List<Component>> components, Action<T> draw) where T : Component {
        if (!components.TryGetValue(typeof(T), out List<Component>? list)) return;

        /// Index loop: no enumerator allocation, and no throw if a component is added mid-frame
        for (int i = 0; i < list.Count; i++) {
            draw((T)list[i]);
        }
    }



    extension(BoxColliderComponent comp) {
        public void DrawGizmo () {
            if (Constants.drawGizmos) {
                RenderData renderInfo = new RenderData() {
                    model = comp.gameObject.Transform.GetWorldMatrix(),

                    mesh = Gizmos._mesh_CubeWireframe,
                    material = Gizmos._mat_GizmosGreen_Instanced,
                    primitiveType = Silk.NET.OpenGL.PrimitiveType.Lines,
                };
                Renderer.Instance.AddRenderData(renderInfo);
            }
        }
    }

    extension(CapsuleColliderComponent comp) {
        public void DrawGizmo () {
            if (Constants.drawGizmos) {
                RenderData renderInfo = new RenderData() {
                    model = comp.gameObject.Transform.GetWorldMatrix(),

                    mesh = Gizmos._mesh_CapsuleWireframe,
                    material = Gizmos._mat_GizmosGreen_Instanced,
                    primitiveType = Silk.NET.OpenGL.PrimitiveType.Lines,
                };
                Renderer.Instance.AddRenderData(renderInfo);
            }
        }
    }

    extension(PlaneColliderComponent comp) {
        public void DrawGizmo () {
            if (Constants.drawGizmos) {
                RenderData renderInfo = new RenderData() {
                    model = comp.gameObject.Transform.GetWorldMatrix(),

                    mesh = Gizmos._mesh_PlaneWireframe,
                    material = Gizmos._mat_GizmosGreen_Instanced,
                    primitiveType = Silk.NET.OpenGL.PrimitiveType.Lines,
                };
                Renderer.Instance.AddRenderData(renderInfo);
            }
        }
    }

    extension(SphereColliderComponent comp) {
        public void DrawGizmo () {
            if (Constants.drawGizmos) {
                RenderData renderInfo = new RenderData() {
                    model = comp.gameObject.Transform.GetWorldMatrix(),

                    mesh = Gizmos._mesh_SphereWireframe,
                    material = Gizmos._mat_GizmosGreen_Instanced,
                    primitiveType = Silk.NET.OpenGL.PrimitiveType.Lines,
                };
                Renderer.Instance.AddRenderData(renderInfo);
            }
        }
    }

}
