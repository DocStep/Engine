using ImGuiNET;

namespace Editor;


public static class ComponentsInpector {

    extension(Component component) {
        public void DrawInspector () {
            Graphics.EditorUI.DrawObject(component);
        }
    }

    extension(Engine.Graphics.MeshComponent component) {
        public void DrawInspector () {
            Graphics.EditorUI.DrawField("Mesh", component.mesh?.Name, isReadonly: true);
            Graphics.EditorUI.DrawField("Shader", component.material?.shader.Name, isReadonly: true);
        }
    }

    extension(PhysicsComponent component) {
        public void DrawInspector () {
            Graphics.EditorUI.DrawField("Mode", component.Rigidbody.Kinematic ? "Kinematic" : "Dynamic", isReadonly: true);
            Graphics.EditorUI.DrawField("Velocity", component.Rigidbody.Velocity.Linear, isReadonly: true);
            Graphics.EditorUI.DrawField("Angular Velocity", component.Rigidbody.Velocity.Angular, isReadonly: true);

            BepuPhysics.BodyInertia inertia = component.Rigidbody.LocalInertia;
            Vector3 invInertia = new Vector3(inertia.InverseInertiaTensor.XX, inertia.InverseInertiaTensor.YY, inertia.InverseInertiaTensor.ZZ);
            Graphics.EditorUI.DrawField("InvInertia", invInertia, isReadonly: true);
        }
    }


    /*extension (Transform comp) {
        public void DrawInspector () {
            Graphics.EditorUI.DrawObject(comp);
        }
    }*/


}
