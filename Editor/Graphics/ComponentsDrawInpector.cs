namespace Editor;


public static class ComponentsDrawInpector {

    extension(Component comp) {
        public void DrawInspector () {
            Graphics.EditorUI.DrawObject(comp);
        }
    }

    extension(Engine.Graphics.MeshComponent comp) {
        public void DrawInspector () {
            ImGuiNET.ImGui.TextDisabled("Mesh: " + comp.mesh?.Name);
            ImGuiNET.ImGui.TextDisabled("Shader: " + comp.material?.shader.Name);
        }
    }

    extension(PhysicsComponent comp) {
        public void DrawInspector () {
            ImGuiNET.ImGui.TextDisabled($"Mode: {comp.Rigidbody.Data.MotionType}");
            ImGuiNET.ImGui.TextDisabled($"Velocity: {comp.Rigidbody.Velocity.ToString3()}");
            ImGuiNET.ImGui.TextDisabled($"Angular Velocity: {comp.Rigidbody.AngularVelocity.ToString3()}");
        }
    }
    

    /*extension (Transform comp) {
        public void DrawInspector () {
            Graphics.EditorUI.DrawObject(comp);
        }
    }*/


}
