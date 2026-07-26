using System;
using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Engine.Graphics;


public class EditorUI : Singleton<EditorUI>, IDisposable {
    public EditorUI () {
        ImGUI = new ImGuiController(Renderer.GL, Engine.Window, Engine.Input);
        Renderer.Instance.de_Dispose += Dispose;

        ImGuiIOPtr io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

        ImGui.LoadIniSettingsFromDisk(io.IniFilename);
    }

    public readonly ImGuiController ImGUI = null!;
    public bool isUIClick = false;
    private bool _isClosing = false;

    public const float valueStep = 0.01f;


    public void Update () {
        if (_isClosing) return;

        ImGUI.Update((float)Engine.deltaTime);

        if (ImGui.GetIO().WantCaptureMouse || ImGui.IsAnyItemActive())
            isUIClick = true;
        else
            isUIClick = false;
    }

    public void Draw () {
        if (_isClosing) return;

        ImGuiViewportPtr viewport = ImGui.GetMainViewport();
        uint dockspaceId = ImGui.DockSpaceOverViewport(0, viewport, ImGuiDockNodeFlags.PassthruCentralNode);
        
        DrawInspector(dockspaceId);
        DrawInfo(dockspaceId);

        ImGUI.Render();

        ImGuiIOPtr io = ImGui.GetIO();
        if (io.WantSaveIniSettings) {
            ImGui.SaveIniSettingsToDisk(io.IniFilename);
            io.WantSaveIniSettings = false;
        }
    }

    private void DrawInspector (uint dockspaceId) {
        ImGui.SetNextWindowDockID(dockspaceId, ImGuiCond.FirstUseEver);
        ImGui.Begin("Inspector");
        
        GameObject? selectedGO = Gizmos._gizmo_Selected.selectedMesh?.owner;
        if (selectedGO is not null) {
            ImGui.Text("Selected:");
            ImGui.InputText("Name", ref selectedGO.Name, int.MaxValue);
            
            ImGui.Text("Transform");
            selectedGO.Transform.DrawInspector();

            for (int c = 0; c < selectedGO.Components.Count; c++) {
                ImGui.Text(selectedGO.Components[c].Name);
                selectedGO.Components[c].DrawInspector();
            }
        }

        ImGui.End();
    }

    private void DrawInfo (uint dockspaceId) {
        ImGui.SetNextWindowDockID(dockspaceId, ImGuiCond.FirstUseEver);
        ImGui.Begin("Info");

        DrawScript(Renderer.Instance.Stats);

        ImGui.End();
    }


    /// For classes / reference types
    public static void DrawScript (object target) {
        Type type = target.GetType();
        
        FieldInfo[] fields = type.GetFields(BindingFlags.Public|BindingFlags.Instance);
        foreach (FieldInfo field in fields) {
            object? value = field.GetValue(target);
            object? drawn = DrawField(field, value);
            if (drawn is not null) field.SetValue(target, drawn);
        }

        /*PropertyInfo[] props = type.GetProperties(BindingFlags.Public|BindingFlags.Instance);
        foreach (PropertyInfo prop in props) {
            if (!prop.CanRead || !prop.CanWrite) continue;
            object? value = prop.GetValue(this);
            object? drawn = EditorUI.DrawField(prop, value);
            if (drawn != null) prop.SetValue(this, drawn);
        }*/
    }

    /// For structs - box, mutate the box, then write back to the ref
    /*public static void DrawScript<T> (ref T target) where T : struct {
        Type type = typeof(T);
        object boxed = target;

        FieldInfo[] fields = type.GetFields(BindingFlags.Public|BindingFlags.Instance);
        foreach (FieldInfo field in fields) {
            object? value = field.GetValue(boxed);
            object? drawn = DrawField(field, value);
            if (drawn is not null) field.SetValue(boxed, drawn);
        }

        *//*PropertyInfo[] props = type.GetProperties(BindingFlags.Public|BindingFlags.Instance);
        foreach (PropertyInfo prop in props) {
            if (!prop.CanRead || !prop.CanWrite) continue;
            object? value = prop.GetValue(boxed);
            object? drawn = EditorUI.DrawField(prop, value);
            if (drawn != null) prop.SetValue(boxed, drawn);
        }*//*

        target = (T)boxed;
    }*/

    /// Draws one ImGui widget based on the runtime type of value, returns the new value if changed, null otherwise
    public static object? DrawField (MemberInfo member, object? value) {
        if (member.GetCustomAttribute<Hide>() is not null) return null;

        string label = member.Name;
        switch (value) {
            case Vector3 v3:
                WrapVector3? clampAtt = member.GetCustomAttribute<WrapVector3>();
                if (clampAtt is not null) label = Utils.StringNameCapital(label);
                float speed = clampAtt is not null ? WrapVector3.Step : valueStep;
                bool changed = ImGui.DragFloat3(label, ref v3, speed);
                if (changed) {
                    if (clampAtt is not null) v3 = Utils.WrapVector3(v3, clampAtt.Min, clampAtt.Max);
                    return v3;
                }
                return null;
            case float f:
                if (ImGui.DragFloat(label, ref f, 0.01f)) return f;
                return null;
            case int i:
                if (ImGui.DragInt(label, ref i)) return i;
                return null;
            case bool b:
                if (ImGui.Checkbox(label, ref b)) return b;
                return null;
            case string s:
                if (ImGui.InputText(label, ref s, 256)) return s;
                return null;
            default:
                return null;
        }
    }




    public void Dispose () {
        if (_isClosing) return;
        _isClosing = true;

        ImGUI.Dispose();
    }

}
