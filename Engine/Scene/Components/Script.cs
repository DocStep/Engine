using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Engine;


public class Script : Component {
    public override string Name => nameof(Script) + ": " + GetType().Name;

/*
    public override void DrawInspector () {
        Type type = GetType();

        PropertyInfo[] props = type.GetProperties(BindingFlags.Public|BindingFlags.Instance);
        foreach (PropertyInfo prop in props) {
            if (!prop.CanRead || !prop.CanWrite) continue;
            object? value = prop.GetValue(this);
            object? drawn = DrawField(prop.Name, value);
            if (drawn != null) prop.SetValue(this, drawn);
        }

        FieldInfo[] fields = type.GetFields(BindingFlags.Public|BindingFlags.Instance);
        foreach (FieldInfo field in fields) {
            object? value = field.GetValue(this);
            object? drawn = DrawField(field.Name, value);
            if (drawn != null) field.SetValue(this, drawn);
        }
    }

    /// Draws one ImGui widget based on the runtime type of value, returns the new value if changed, null otherwise
    private object? DrawField (string label, object? value) {
        switch (value) {
            case Vector3 v3:
                if (ImGuiNET.ImGui.DragFloat3(label, ref v3, 0.01f)) return v3;
                return null;
            case float f:
                if (ImGuiNET.ImGui.DragFloat(label, ref f, 0.01f)) return f;
                return null;
            case int i:
                if (ImGuiNET.ImGui.DragInt(label, ref i)) return i;
                return null;
            case bool b:
                if (ImGuiNET.ImGui.Checkbox(label, ref b)) return b;
                return null;
            case string s:
                if (ImGuiNET.ImGui.InputText(label, ref s, 256)) return s;
                return null;
            default:
                return null;
        }
    }
*/
}
