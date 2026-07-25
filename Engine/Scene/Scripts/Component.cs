using System.Reflection;
using Newtonsoft.Json;

namespace Engine;


public abstract class Component : ISavable {
    public Component () { }

    public readonly Guid? Guid = lib.Guid;
    public readonly long? Id = lib.Id;

    [JsonIgnore] public GameObject owner = null!;
    public Guid? ownerGuid = null;
    [JsonIgnore] public Transform? parent = null;
    public Guid? parentGuid = null;
    public abstract string Name { get; }


    //protected string? typeName = null;


    public virtual void SetParent (GameObject gameObject) {
        owner = gameObject;
    }

    public virtual void OnAdd () { }
    public virtual void OnRemove () { }



    public virtual void DrawInspector () {
        Type type = GetType();

        PropertyInfo[] props = type.GetProperties(BindingFlags.Public|BindingFlags.Instance);
        foreach (PropertyInfo prop in props) {
            if (!prop.CanRead || !prop.CanWrite) continue;
            object? value = prop.GetValue(this);
            object? drawn = DrawField(prop, value);
            if (drawn != null) prop.SetValue(this, drawn);
        }

        FieldInfo[] fields = type.GetFields(BindingFlags.Public|BindingFlags.Instance);
        foreach (FieldInfo field in fields) {
            object? value = field.GetValue(this);
            object? drawn = DrawField(field, value);
            if (drawn is not null) field.SetValue(this, drawn);
        }
    }

    public const float valueStep = 0.01f;

    /// Draws one ImGui widget based on the runtime type of value, returns the new value if changed, null otherwise
    private object? DrawField (MemberInfo member, object? value) {
        string label = member.Name;
        switch (value) {
            case Vector3 v3:
                AttributeClampRotation? clampAtt = member.GetCustomAttribute<AttributeClampRotation>();
                bool changed = ImGuiNET.ImGui.DragFloat3(label, ref v3, clampAtt is not null ? AttributeClampRotation.step : valueStep);
                if (changed) {
                    if (clampAtt is not null) v3 = clampAtt.Update(v3);
                    return v3;
                }
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



    public void PreSave () {
        /// Own
        /// ...
    }
    //public abstract JObj ToJObj ();

    public virtual void PostLoad () { }
    /*public static T? ToComponent<T> (JObj jObj) where T : Component {
        return jObj.Data as T;
    }*/

}
