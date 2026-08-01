namespace Editor.Graphics;


public static class ComponentsDraw {

    extension(BoxColliderComponent comp) {
        public void DrawGizmo () {
            if (comp.drawGizmos) {
                Engine.Graphics.RenderInfo renderInfo = new Engine.Graphics.RenderInfo() {
                    pos = comp.Position + comp.owner.Transform.Position,
                    rot = comp.owner.Transform.Rotation,
                    scale = comp.Scale*comp.owner.Transform.Scale,

                    mesh = Gizmos._mesh_CubeWireframe,
                    material = Gizmos._mat_GizmosGreen,
                    primitiveType = Silk.NET.OpenGL.PrimitiveType.Lines,
                };
                Engine.Graphics.Renderer.Instance.AddRenderInfo(renderInfo);
            }
        }
    }

    extension(CapsuleColliderComponent comp) {
        public void DrawGizmo () {
            if (comp.drawGizmos) {
                Engine.Graphics.RenderInfo renderInfo = new Engine.Graphics.RenderInfo() {
                    pos = comp.Position + comp.owner.Transform.Position,
                    rot = comp.owner.Transform.Rotation,
                    scale = comp.owner.Transform.Scale,

                    mesh = Gizmos._mesh_CapsuleWireframe,
                    material = Gizmos._mat_GizmosGreen,
                    primitiveType = Silk.NET.OpenGL.PrimitiveType.Lines,
                };
                Engine.Graphics.Renderer.Instance.AddRenderInfo(renderInfo);
            }
        }
    }

    extension(PlaneColliderComponent comp) {
        public void DrawGizmo () {
            if (comp.drawGizmos) {
                Engine.Graphics.RenderInfo renderInfo = new Engine.Graphics.RenderInfo() {
                    pos = comp.Position + comp.owner.Transform.Position,
                    rot = comp.owner.Transform.Rotation,
                    scale = comp.owner.Transform.Scale,

                    mesh = Gizmos._mesh_PlaneWireframe,
                    material = Gizmos._mat_GizmosGreen,
                    primitiveType = Silk.NET.OpenGL.PrimitiveType.Lines,
                };
                Engine.Graphics.Renderer.Instance.AddRenderInfo(renderInfo);
            }
        }
    }

    extension(SphereColliderComponent comp) {
        public void DrawGizmo () {
            if (comp.drawGizmos) {
                Engine.Graphics.RenderInfo renderInfo = new Engine.Graphics.RenderInfo() {
                    pos = comp.Position + comp.owner.Transform.Position,
                    rot = comp.owner.Transform.Rotation,
                    scale = 2f*comp.Radius*comp.owner.Transform.Scale,

                    mesh = Gizmos._mesh_SphereWireframe,
                    material = Gizmos._mat_GizmosGreen,
                    primitiveType = Silk.NET.OpenGL.PrimitiveType.Lines,
                };
                Engine.Graphics.Renderer.Instance.AddRenderInfo(renderInfo);
            }
        }
    }
    

    /*extension (Transform comp) {
        public void DrawInspector () {
            Graphics.EditorUI.DrawObject(comp);
        }
    }*/


}
