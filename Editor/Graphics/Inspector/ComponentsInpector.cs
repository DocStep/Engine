using ImGuiNET;

namespace Editor;


public static class ComponentsInpector {

    extension(Component component) {
        public void DrawInspector () {
            Graphics.EditorUI.DrawObject(component);
        }
    }

    extension(PhysicsComponent component) {
        public void DrawInspector () {
            ((Component)component).DrawInspector();

            Graphics.EditorUI.DrawLabel("Mode", component.Rigidbody.Kinematic ? "Kinematic" : "Dynamic");
            Graphics.EditorUI.DrawLabel("Velocity", component.Rigidbody.Velocity.Linear);
            Graphics.EditorUI.DrawLabel("Angular Velocity", component.Rigidbody.Velocity.Angular);

            BepuPhysics.BodyInertia inertia = component.Rigidbody.LocalInertia;
            Vector3 invInertia = new Vector3(inertia.InverseInertiaTensor.XX, inertia.InverseInertiaTensor.YY, inertia.InverseInertiaTensor.ZZ);
            Graphics.EditorUI.DrawLabel("InvInertia", invInertia);
        }
    }


}
