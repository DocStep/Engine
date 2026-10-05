using Engine.Graphics;

namespace Editor.Graphics;


public static class ComponentsGizmo {

    extension(BoxColliderComponent comp) {
        public void DrawGizmo () {
            if (Constants.drawGizmos) {
                RenderData renderInfo = new RenderData() {
                    model = comp.gameObject.Transform.GetWorldMatrix(),

                    mesh = Gizmos._mesh_CubeWireframe,
                    material = Gizmos._mat_GizmosGreen,
                    primitiveType = Silk.NET.OpenGL.PrimitiveType.Lines,
                };
                Renderer.Instance.AddRenderData(renderInfo);
            }
        }
    }

    //extension(CapsuleColliderComponent comp) {
    //    public void DrawGizmo () {
    //        if (Constants.drawGizmos) {
    //            RenderData renderInfo = new RenderData() {
    //                model = comp.gameObject.Transform.GetWorldMatrix(),

    //                mesh = Gizmos._mesh_CapsuleWireframe,
    //                material = Gizmos._mat_GizmosGreen,
    //                primitiveType = Silk.NET.OpenGL.PrimitiveType.Lines,
    //            };
    //            Renderer.Instance.AddRenderData(renderInfo);
    //        }
    //    }
    //}

    //extension(PlaneColliderComponent comp) {
    //    public void DrawGizmo () {
    //        if (Constants.drawGizmos) {
    //            RenderData renderInfo = new RenderData() {
    //                model = comp.gameObject.Transform.GetWorldMatrix(),

    //                mesh = Gizmos._mesh_PlaneWireframe,
    //                material = Gizmos._mat_GizmosGreen,
    //                primitiveType = Silk.NET.OpenGL.PrimitiveType.Lines,
    //            };
    //            Renderer.Instance.AddRenderData(renderInfo);
    //        }
    //    }
    //}

    //extension(SphereColliderComponent comp) {
    //    public void DrawGizmo () {
    //        if (Constants.drawGizmos) {
    //            RenderData renderInfo = new RenderData() {
    //                model = comp.gameObject.Transform.GetWorldMatrix(),

    //                mesh = Gizmos._mesh_SphereWireframe,
    //                material = Gizmos._mat_GizmosGreen,
    //                primitiveType = Silk.NET.OpenGL.PrimitiveType.Lines,
    //            };
    //            Renderer.Instance.AddRenderData(renderInfo);
    //        }
    //    }
    //}
    

    /*extension (Transform comp) {
        public void DrawInspector () {
            Graphics.EditorUI.DrawObject(comp);
        }
    }*/


}
