using ImGuiNET;
using System.Reflection;

namespace Editor;


public static class ComponentsInpector {

    private static Transform? _eulerDragTransform = null;
    private static Vector3 _eulerDragValue = Vector3.Zero;
    private static string _format = "0.##" ;
    private static readonly Dictionary<Transform, (Quaternion rotation, Vector3 euler)> _eulerDisplayCache = new();


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

            string label = nameof(transform.LocalEuler);
            Graphics.EditorUI.InvertedOrder(ref label);
            if (ImGui.DragFloat3(label, ref draggedEuler, 1f, 0f, 0f, "%.2f")) {
                Vector3 delta = new Vector3(
                    ShortestAngle(localEuler.X, draggedEuler.X),
                    ShortestAngle(localEuler.Y, draggedEuler.Y),
                    ShortestAngle(localEuler.Z, draggedEuler.Z)
                );
                transform.RotateLocalEuler(delta);
            }

            Vector3 localScale = transform.LocalScale;
            Graphics.EditorUI.DrawVar(nameof(transform.LocalScale), ref localScale);
            transform.LocalScale = localScale;
        }
    }

    extension(Engine.Graphics.UI.RectTransform component) {
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

    private static float ShortestAngle (float from, float to) {
        float delta = (to - from)%360f;

        if (180f < delta) delta -= 360f;
        if (delta < -180f) delta += 360f;

        return delta;
    }

    private static Vector3 GetDisplayEuler (Transform transform) {
        Quaternion rotation = transform.LocalQuaternion;

        if (_eulerDragTransform == transform)
            return _eulerDragValue;

        if (_eulerDisplayCache.TryGetValue(transform, out (Quaternion rotation, Vector3 euler) cached)
            && Quaternion.Dot(cached.rotation, rotation) > 0.99999f)
            return cached.euler;

        Vector3 euler = transform.LocalEuler;
        _eulerDisplayCache[transform] = (rotation, euler);
        return euler;
    }

    private static IEnumerable<Attribute> GetPropertyAttributes<T> (string propertyName) {
        return typeof(T)
            .GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetCustomAttributes();
    }

}
