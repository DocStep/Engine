using ImGuiNET;

namespace Editor;


public static class ComponentsInpector {

    extension(Component component) {
        public void DrawInspector () {
            bool temp_b = component.Enabled;
            if (ImGui.Checkbox("##" + nameof(component.Enabled), ref temp_b)) {
                component.Enabled = temp_b;
            }
            Graphics.EditorUI.DrawObject(component);
        }
    }

    extension(Transform component) {
        public void DrawInspector () {
            Graphics.EditorUI.DrawObject(component);
        }
    }

    extension(PhysicsComponent component) {
        public void DrawInspector () {
            ((Component)component).DrawInspector();

            Graphics.EditorUI.DrawField("Mode", component.Rigidbody.Kinematic ? "Kinematic" : "Dynamic", isReadonly: true);
            Graphics.EditorUI.DrawField("Velocity", component.Rigidbody.Velocity.Linear, isReadonly: true);
            Graphics.EditorUI.DrawField("Angular Velocity", component.Rigidbody.Velocity.Angular, isReadonly: true);

            BepuPhysics.BodyInertia inertia = component.Rigidbody.LocalInertia;
            Vector3 invInertia = new Vector3(inertia.InverseInertiaTensor.XX, inertia.InverseInertiaTensor.YY, inertia.InverseInertiaTensor.ZZ);
            Graphics.EditorUI.DrawField("InvInertia", invInertia, isReadonly: true);
        }
    }


}
