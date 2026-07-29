using System;
using System.Numerics;
using System.Reflection;
using Marshal = System.Runtime.InteropServices.Marshal;
using ImGuiNET;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Engine.Graphics;


public class EditorUI : Singleton<EditorUI>, IDisposable {
    public EditorUI () {
        ImGUI = new ImGuiController(Renderer.GL, Engine.Window, Engine.Input);
        Renderer.Instance.de_Dispose += Dispose;

        SetDock();
        //SetFont(AssetsEngine._fontData);

        ImGui.LoadIniSettingsFromDisk(ImGui.GetIO().IniFilename);
    }

    public ImGuiController ImGUI = null!;
    public bool isUIClick = false;
    private bool _isClosing = false;

    public const float valueStep = 0.01f;

    private Vector2 _sceneAvail = new Vector2(1280, 720);
    public Vector2 SceneAvail => _sceneAvail;

    private Vector2 _sceneRectMin;
    private Vector2 _sceneRectMax;
    public bool IsSceneHovered { get; private set; }


    public void Update () {
        if (_isClosing) return;

        ImGUI.Update((float)Engine.deltaTime);

        bool wantCapture = ImGui.GetIO().WantCaptureMouse || ImGui.IsAnyItemActive();
        isUIClick = wantCapture && !IsSceneHovered;
    }

    public void Draw () {
        if (_isClosing) return;

        ImGuiViewportPtr viewport = ImGui.GetMainViewport();
        uint dockspaceId = ImGui.DockSpaceOverViewport(0, viewport, ImGuiDockNodeFlags.PassthruCentralNode);

        DrawSceneView(dockspaceId);
        DrawInspector(dockspaceId);
        DrawInfo(dockspaceId);

        ImGUI.Render();

        ImGuiIOPtr io = ImGui.GetIO();
        if (io.WantSaveIniSettings) {
            ImGui.SaveIniSettingsToDisk(io.IniFilename);
            io.WantSaveIniSettings = false;
        }
    }

    private void DrawSceneView (uint dockspaceId) {
        ImGui.SetNextWindowDockID(dockspaceId, ImGuiCond.FirstUseEver);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.Begin("Scene");

        Vector2 avail = ImGui.GetContentRegionAvail();
        _sceneAvail = new Vector2(MathF.Max(avail.X, 1f), MathF.Max(avail.Y, 1f));

        ImGui.Image((IntPtr)Renderer.Instance.PostProcessStack.OutputTexture,
            _sceneAvail, new Vector2(0, 1), new Vector2(1, 0));

        _sceneRectMin = ImGui.GetItemRectMin();
        _sceneRectMax = ImGui.GetItemRectMax();
        IsSceneHovered = ImGui.IsItemHovered();

        ImGui.End();
        ImGui.PopStyleVar();
    }
    /// Mouse position remapped into Scene FBO pixel space, or null if outside the panel
    public Vector2? GetSceneMousePos (Vector2 screenMousePos) {
        if (!IsSceneHovered) return null;

        Vector2 local = screenMousePos - _sceneRectMin;
        if (local.X < 0 || local.Y < 0 || local.X > _sceneAvail.X || local.Y > _sceneAvail.Y)
            return null;

        return local;
    }

    private void DrawInspector (uint dockspaceId) {
        ImGui.SetNextWindowDockID(dockspaceId, ImGuiCond.FirstUseEver);
        ImGui.Begin("Inspector");

        GameObject? selectedGO = Gizmos._gizmo_Selected.selectedMesh?.owner;
        if (selectedGO is not null) {
            ImGui.Text("Selected:");
            ImGui.InputText("Name", ref selectedGO.Name, int.MaxValue);

            ImGui.Text("Transform");
            ImGui.PushID(selectedGO.Transform.GetHashCode());
            selectedGO.Transform.DrawInspector();
            ImGui.PopID();

            for (int c = 0; c < selectedGO.Components.Count; c++) {
                ImGui.Text(selectedGO.Components[c].Name);
                ImGui.PushID(selectedGO.Components[c].GetHashCode());
                selectedGO.Components[c].DrawInspector();
                ImGui.PopID();
            }
        }

        ImGui.End();
    }

    private void DrawInfo (uint dockspaceId) {
        ImGui.SetNextWindowDockID(dockspaceId, ImGuiCond.FirstUseEver);
        ImGui.Begin("GL Info");
        ImGui.BeginDisabled();

        DrawObject(Renderer.Instance.Stats);
        ImGui.NewLine();
        DrawObject(Shader.Stats);

        ImGui.EndDisabled();
        ImGui.End();
    }


    /// For classes / reference types
    public static void DrawObject (object target) {
        Type type = target.GetType();
        
        FieldInfo[] fields = type.GetFields(BindingFlags.Public|BindingFlags.Instance);
        foreach (FieldInfo field in fields) {
            object? value = field.GetValue(target);
            object? drawn = DrawField(field, value);
            if (drawn is not null) field.SetValue(target, drawn);
        }

        PropertyInfo[] props = type.GetProperties(BindingFlags.Public|BindingFlags.Instance);
        foreach (PropertyInfo prop in props) {
            if (!prop.CanRead || !prop.CanWrite) continue;
            object? value = prop.GetValue(target);
            object? drawn = EditorUI.DrawField(prop, value);
            if (drawn != null) prop.SetValue(target, drawn);
        }
    }

    /*/// For structs - box, mutate the box, then write back to the ref
    public static void DrawScript<T> (ref T target) where T : struct {
        Type type = typeof(T);
        object boxed = target;

        FieldInfo[] fields = type.GetFields(BindingFlags.Public|BindingFlags.Instance);
        foreach (FieldInfo field in fields) {
            object? value = field.GetValue(boxed);
            object? drawn = DrawField(field, value);
            if (drawn is not null) field.SetValue(boxed, drawn);
        }

        //PropertyInfo[] props = type.GetProperties(BindingFlags.Public|BindingFlags.Instance);
        //foreach (PropertyInfo prop in props) {
        //    if (!prop.CanRead || !prop.CanWrite) continue;
        //    object? value = prop.GetValue(boxed);
        //    object? drawn = EditorUI.DrawField(prop, value);
        //    if (drawn != null) prop.SetValue(boxed, drawn);
        //}

        target = (T)boxed;
    }*/

    /// Draws one ImGui widget based on the runtime type of value, returns the new value if changed, null otherwise
    public static object? DrawField (MemberInfo member, object? value) {
        if (member.GetCustomAttribute<Hide>() is not null) return null;

        object? result = null;
        string label = member.Name;
        label = Utils.NameCapital(label);

        bool isReadonly = member.GetCustomAttribute<Readonly>() is not null;
        if (isReadonly) ImGui.BeginDisabled(true);

        switch (value) {
            case Vector3 v3:
                WrapRotation? clampAtt = member.GetCustomAttribute<WrapRotation>();
                float speed = clampAtt is not null ? WrapRotation.Step : valueStep;
                if (ImGui.DragFloat3(label, ref v3, speed, 0, 0, "%.2f")) {
                    if (clampAtt is not null) v3 = Utils.WrapVector3(v3, clampAtt.Min, clampAtt.Max);
                    result = v3;
                }
                break;
            case float f:
                if (ImGui.DragFloat(label, ref f, valueStep, 0, 0, "%.2f")) result = f;
                break;
            case int i:
                if (ImGui.DragInt(label, ref i)) result = i;
                break;
            case bool b:
                if (ImGui.Checkbox(label, ref b)) result = b;
                break;
            case string s:
                if (ImGui.InputText(label, ref s, 256)) result = s;
                break;
        }

        if (isReadonly) ImGui.EndDisabled();

        return result;
    }
    private static void DragDisabledFloat (string name, ref float value) {
        ImGui.Text(name);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1f);

        ImGui.BeginDisabled(true);
        ImGui.DragFloat($"##{name}", ref value);
        ImGui.EndDisabled();
    }
    private static void DragDisabledInt (string name, ref int value) {
        ImGui.Text(name);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1f);

        ImGui.BeginDisabled(true);
        ImGui.DragInt($"##{name}", ref value);
        ImGui.EndDisabled();
    }
    private static void DragDisabledBool (string name, ref bool value) {
        ImGui.Text(name);
        ImGui.SameLine();

        ImGui.BeginDisabled(true);
        ImGui.Checkbox($"##{name}", ref value);
        ImGui.EndDisabled();
    }
    private static void DragDisabledString (string name, ref string value) {
        ImGui.Text(name);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1f);

        ImGui.BeginDisabled(true);
        ImGui.InputText($"##{name}", ref value, 256);
        ImGui.EndDisabled();
    }
    private static void DragDisabledFloat3 (string name, ref Vector3 value) {
        ImGui.Text(name);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1f);

        ImGui.BeginDisabled(true);
        ImGui.DragFloat3($"##{name}", ref value);
        ImGui.EndDisabled();
    }


    public void SetDock () {
        ImGuiIOPtr io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
    }
    public unsafe void SetFont (byte[] fontData) {
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.Clear();

        IntPtr fontPtr = Marshal.AllocHGlobal(fontData.Length);
        Marshal.Copy(fontData, 0, fontPtr, fontData.Length);

        io.Fonts.AddFontFromMemoryTTF(fontPtr, fontData.Length, 16.0f);
        /// don't free fontPtr — ImGui takes ownership (FontDataOwnedByAtlas defaults true) and frees it internally

        io.Fonts.Build();

        /// re-upload the atlas texture to the GPU — this is backend-specific
        var method = typeof(ImGuiController).GetMethod("RecreateFontDeviceTexture", BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(ImGUI, null);
        //ImGUI?.Dispose();
        //ImGUI = new ImGuiController(Renderer.GL, Engine.Window, Engine.Input);
    }



    public void Dispose () {
        if (_isClosing) return;
        _isClosing = true;

        ImGUI.Dispose();
    }

}
