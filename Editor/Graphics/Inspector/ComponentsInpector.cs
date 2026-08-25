using ImGuiNET;

namespace Editor;


public static class ComponentsInpector {

    extension(Component component) {
        public void DrawInspector () {
            Graphics.EditorUI.DrawObject(component);
        }
    }

    extension(Transform transform) {
        public void DrawInspector () {
            Vector3 localPosition = transform.LocalPosition;
            Graphics.EditorUI.DrawVar(nameof(transform.LocalPosition), ref localPosition);
            transform.LocalPosition = localPosition;

            Vector3 localEuler = transform.LocalEuler;
            Vector3 draggedEuler = localEuler;
            if (ImGui.DragFloat3(nameof(transform.LocalEuler), ref draggedEuler, 1f, 0f, 0f, "%.2f")) {
                Vector3 delta = new Vector3(
                    ShortestAngle(localEuler.X, draggedEuler.X),
                    ShortestAngle(localEuler.Y, draggedEuler.Y),
                    ShortestAngle(localEuler.Z, draggedEuler.Z)
                );
                transform.RotateLocal(delta);
            }

            Vector3 localScale = transform.LocalScale;
            Graphics.EditorUI.DrawVar(nameof(transform.LocalScale), ref localScale);
            transform.LocalScale = localScale;
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

    private static float ShortestAngle (float from, float to) {
        float delta = (to - from)%360f;

        if (180f < delta) delta -= 360f;
        if (delta < -180f) delta += 360f;

        return delta;
    }


}
