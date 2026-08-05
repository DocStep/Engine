using System;
using System.Numerics;
using System.Reflection;
using Marshal = System.Runtime.InteropServices.Marshal;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using ImGuiNET;
using Engine;
using Engine.Graphics;

namespace Editor.Graphics;


public class EditorUI : Singleton<EditorUI>, IDisposable {

    protected override void Init () {
        ImGUI = new ImGuiController(Renderer.GL, Windows.Window, Windows.Input);

        SetDock();
        EditorUIStyle.SetAccentColor();
        //SetFont(AssetsEngine._fontData);

        ImGui.LoadIniSettingsFromDisk(ImGui.GetIO().IniFilename);

        Engine.Engine.Instance.de_Update += Update;
        Renderer.Instance.de_LateUpdate += Gizmos.Update;

        Engine.Engine.Instance.de_Render += Draw;
        Renderer.Instance.de_Dispose += Dispose;

        new CameraEditor();
    }

    public ImGuiController ImGUI = null!;
    public bool isUIClick = false;
    private bool _docked = false;
    private bool _isClosing = false;

    public const float valueStep = 0.01f;


    private Vector2 sceneAvail = new Vector2(1280, 720);
    public Vector2 SceneAvail => sceneAvail;

    private Vector2 _sceneRectMin;
    private Vector2 _sceneRectMax;
    public bool isSceneUIHovered { get; private set; }
    public bool isMouseHooked () {
        Vector2 availSize = ImGui.GetContentRegionAvail();
        Vector2 elementPos = ImGui.GetCursorScreenPos();
        return true;
    }


    public void Update () {
        if (_isClosing) return;

        ImGUI.Update((float)Time.deltaTime);

        bool wantCapture = ImGui.GetIO().WantCaptureMouse || ImGui.IsAnyItemActive();
        isUIClick = wantCapture && !isSceneUIHovered;
        //isUIClick = wantCapture;
    }

    public void Draw () {
        if (_isClosing) return;

        /// Switch to the real backbuffer for ImGui — dockspace, panels, and the Scene image itself
        Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        Renderer.GL.DrawBuffer(GLEnum.Back);
        Renderer.GL.Viewport(0, 0, (uint)Windows.Window.Size.X, (uint)Windows.Window.Size.Y);
        Renderer.GL.ClearColor(Constants.clearColor.X, Constants.clearColor.Y, Constants.clearColor.Z, 1f);
        Renderer.GL.Clear((uint)ClearBufferMask.ColorBufferBit);

        ImGuiViewportPtr viewport = ImGui.GetMainViewport();
        //uint dockspaceId = ImGui.DockSpaceOverViewport(0, viewport, ImGuiDockNodeFlags.PassthruCentralNode);
        uint dockspaceId = ImGui.GetID("MainDockspace");
        ImGui.DockSpaceOverViewport(dockspaceId, viewport, ImGuiDockNodeFlags.PassthruCentralNode);

        ImGui.SetNextWindowDockID(dockspaceId, ImGuiCond.FirstUseEver);

        DrawSceneView(dockspaceId);
        DrawInspector(dockspaceId);
        DrawEngineInfo(dockspaceId);
        DrawGLInfo(dockspaceId);
        //ImGui.ShowMetricsWindow();

        ImGUI.Render();

        ImGuiIOPtr io = ImGui.GetIO();
        if (io.WantSaveIniSettings) {
            ImGui.SaveIniSettingsToDisk(io.IniFilename);
            io.WantSaveIniSettings = false;
        }

        _docked = true;
    }

    private void DrawSceneView (uint dockspaceId) {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.Begin("Scene");

        if (_docked) sceneAvail = getSceneAvail();

        ImGui.Image((IntPtr)Renderer.Instance.PostProcess.OutputTexture, sceneAvail, new Vector2(0, 1), new Vector2(1, 0));

        _sceneRectMin = ImGui.GetItemRectMin();
        _sceneRectMax = ImGui.GetItemRectMax();
        isSceneUIHovered = ImGui.IsItemHovered();

        if (!Engine.Input.Inputs.isMouseVisible) {
            Vector2 mousePos_Scene = Engine.Input.WindowInput.Mouse!.Position - ImGui.GetItemRectMin();
            float deltaX = MathF.Floor(mousePos_Scene.X/sceneAvail.X)*sceneAvail.X;
            float deltaY = MathF.Floor(mousePos_Scene.Y/sceneAvail.Y)*sceneAvail.Y;
            Vector2 delta = new Vector2(deltaX, deltaY);
            if (0 < delta.LengthSquared()) {
                Engine.Input.WindowInput.TeleportMouseDelta(-delta);
                //Log.log(sceneAvail, mousePos_Scene, isSceneUIHovered, delta);
            }
        }

        //Log.log("sceneAvail", sceneAvail, "mousePos_Scene", mousePos_Scene, "isUIHovered", isSceneUIHovered);
        //Log.log("mousePos_Scene", mousePos_Scene);

        ImGui.End();
        ImGui.PopStyleVar();
    }

    public Vector2 getSceneAvail () {
        Vector2 avail = ImGui.GetContentRegionAvail();
        Vector2 _sceneAvail = new Vector2(MathF.Max(MathF.Floor(avail.X), 1), MathF.Max(MathF.Floor(avail.Y), 1));
        return _sceneAvail;
    }

    /// Mouse position remapped into Scene FBO pixel space, or null if outside the panel
    public bool GetSceneMousePos (Vector2 mousePosWindow, out Vector2 local) {
        local = mousePosWindow - _sceneRectMin;

        if (!isSceneUIHovered) return false;
        if (local.X < 0 || local.Y < 0 || sceneAvail.X < local.X || sceneAvail.Y < local.Y) return false;

        return true;
    }

    private void DrawInspector (uint dockspaceId) {
        ImGui.Begin("Inspector");

        GameObject? selectedGO = Gizmos._gizmo_Selected.selectedMeshComp?.owner;
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

    private void DrawEngineInfo (uint dockspaceId) {
        ImGui.Begin("Engine Info");
        ImGui.BeginDisabled();

        DrawObject(Engine.Engine.Instance.Stats);

        ImGui.EndDisabled();
        ImGui.End();
    }

    private void DrawGLInfo (uint dockspaceId) {
        ImGui.Begin("Renderer Info");
        ImGui.BeginDisabled();

        Renderer.Instance.Stats.SceneSize = sceneAvail;
        DrawObject(Renderer.Instance.Stats);
        ImGui.NewLine();
        DrawObject(Engine.Graphics.Shader.Stats);

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

    /// For structs - box, mutate the box, then write back to the ref
    /*public static void DrawObject<T> (ref T target) where T : struct {
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

        float step = valueStep;
        ChangeStep? changeSpeed = member.GetCustomAttribute<ChangeStep>();
        if (changeSpeed is not null) step = changeSpeed.Step;

        WrapRotation? clampAtt = member.GetCustomAttribute<WrapRotation>();

        switch (value) {
            case int i:
                if (ImGui.DragInt(label, ref i)) result = i;
                break;
            case long l:
                int temp_i = (int)l;
                if (ImGui.DragInt(label, ref temp_i)) result = (long)temp_i;
                break;
            case float f:
                if (ImGui.DragFloat(label, ref f, step, 0, 0, "%.2f")) result = f;
                break;
            case double d:
                float temp_f = (float)d;
                if (ImGui.DragFloat(label, ref temp_f, step, 0, 0, "%.2f")) result = (double)temp_f;
                break;
            case bool b:
                if (ImGui.Checkbox(label, ref b)) result = b;
                break;
            case string s:
                if (ImGui.InputText(label, ref s, 256)) result = s;
                break;
            case Guid g:
                string temp_s = g.ToString();
                if (ImGui.InputText(label, ref temp_s, 256)) result = temp_s;
                break;
            case Vector2 v2:
                if (ImGui.DragFloat2(label, ref v2, step, 0, 0, "%.2f")) {
                    if (clampAtt is not null) v2 = Utils.WrapVector3(v2, clampAtt.Min, clampAtt.Max);
                    result = v2;
                }
                break;
            case Vector3 v3:
                if (ImGui.DragFloat3(label, ref v3, step, 0, 0, "%.2f")) {
                    if (clampAtt is not null) v3 = Utils.WrapVector3(v3, clampAtt.Min, clampAtt.Max);
                    result = v3;
                }
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
    
    /// <> <?>
    public /*unsafe*/ void SetFont (byte[] fontData) {
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
