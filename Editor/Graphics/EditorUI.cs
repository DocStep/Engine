using System;
using System.Numerics;
using System.Linq;
using System.Reflection;
using Marshal = System.Runtime.InteropServices.Marshal;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using ImGuiNET;
using Engine;
using Engine.Graphics;
using Engine.Input;
using Editor;

namespace Editor.Graphics;


public class EditorUI : Singleton<EditorUI>, IDisposable {

    protected override void Init () {
        ImGUI = new ImGuiController(Renderer.GL, Windows.Window, Windows.Input);

        SetDock();
        EditorUIStyle.SetAccentColor();
        //SetFont(AssetsEngine._fontData);

        ImGui.LoadIniSettingsFromDisk(ImGui.GetIO().IniFilename);

        Inputs.de_UpdateInput += UpdateInput;

        Engine.Engine.Instance.de_Update += Update;
        Renderer.Instance.de_LateUpdate += Gizmos.Update;

        Engine.Engine.Instance.de_Render += Draw;
        Renderer.Instance.de_Dispose += Dispose;

        new CameraEditor();

        Draw();

        //ComponentManager.Instance.de_RegisterType += RegisterType;
        RegisterTypes();
    }

    public ImGuiController ImGUI = null!;

    private static readonly Dictionary<Type, Action<Component>> _drawers = new Dictionary<Type, Action<Component>>();

    public bool isUIClick { get; private set; } = false;
    private bool _docked = false;
    private bool _dockBuilt = false;
    private bool _isClosing = false;

    public const bool drawInverted = true;
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
        Renderer.GL.Viewport(0, 0, (uint)Renderer.Instance.Stats.WindowSize.X, (uint)Renderer.Instance.Stats.WindowSize.Y);
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

        EditorUI.Instance.UpdateAvail();
        ImGui.Image((IntPtr)Renderer.Instance.PostProcess.OutputTexture, sceneAvail, new Vector2(0, 1), new Vector2(1, 0));

        _sceneRectMin = ImGui.GetItemRectMin();
        _sceneRectMax = ImGui.GetItemRectMax();
        isSceneUIHovered = ImGui.IsItemHovered();
            
        if (!Inputs.isMouseVisible) {
            Vector2 mousePos_Scene = WindowInput.Mouse!.Position - ImGui.GetItemRectMin();
            float deltaX = MathF.Floor(mousePos_Scene.X/sceneAvail.X)*sceneAvail.X;
            float deltaY = MathF.Floor(mousePos_Scene.Y/sceneAvail.Y)*sceneAvail.Y;
            Vector2 delta = new Vector2(deltaX, deltaY);
            if (0 < delta.LengthSquared()) {
                WindowInput.TeleportMouseDelta(-delta);
                //Log.log(sceneAvail, mousePos_Scene, isSceneUIHovered, delta);
            }
        }

        //Log.log("sceneAvail", sceneAvail, "mousePos_Scene", mousePos_Scene, "isUIHovered", isSceneUIHovered);
        //Log.log("mousePos_Scene", mousePos_Scene);

        ImGui.End();
        ImGui.PopStyleVar();
    }

    private void DrawInspector (uint dockspaceId) {
        ImGui.Begin("Inspector");

        GameObject? selectedGO = Gizmos._gizmo_Selected.selectedMeshComp?.owner;
        if (selectedGO is not null) {
            ImGui.Text("Selected:");
            ImGui.InputText("Name", ref selectedGO.Name, int.MaxValue);

            ImGui.PushID("Transform");
            if (ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen)) {
                selectedGO.Transform.DrawInspector();
            }
            ImGui.PopID();

            for (int c = 0; c < selectedGO.Components.Count; c++) {
                Component comp = selectedGO.Components[c];
                ImGui.PushID(comp.GetHashCode());
                if (ImGui.CollapsingHeader(comp.Name, ImGuiTreeNodeFlags.DefaultOpen)) {
                    if (_drawers.TryGetValue(comp.GetType(), out var draw)) {
                        draw(comp);
                    } else {
                        comp.DrawInspector();
                    }
                }
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

        DrawObject(Renderer.Instance.Stats);
        ImGui.NewLine();
        DrawObject(Engine.Graphics.Shader.Stats);

        ImGui.EndDisabled();
        ImGui.End();
    }


    public static void DrawObject (object target) {
        ImGui.PushID(target.GetHashCode());

        if (drawInverted) {
            if (ImGui.BeginTable("##inspectorTable", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.NoSavedSettings)) {
                ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthStretch, 0.4f);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                foreach (MemberInfo member in GetMembersInOrder(target.GetType()))
                    DrawMember(target, member);

                ImGui.EndTable();
            }
        } else {
            foreach (MemberInfo member in GetMembersInOrder(target.GetType()))
                DrawMember(target, member);
        }

        ImGui.PopID();
    }
    private static void DrawMember (object target, MemberInfo member) {
        object? value = member switch {
            FieldInfo f => f.GetValue(target),
            PropertyInfo p when p.CanRead => p.GetValue(target),
            _ => null,
        };

        object? drawn = DrawField(member, value);
        if (drawn is null) return;

        switch (member) {
            case FieldInfo f: f.SetValue(target, drawn); break;
            case PropertyInfo p when p.CanWrite: p.SetValue(target, drawn); break;
        }
    }

    public static IEnumerable<MemberInfo> GetMembersInOrder (Type type) {
        List<MemberInfo> result = new();
        List<Type> types = new();

        while (type != null && type != typeof(object)) {
            types.Insert(0, type);
            type = type.BaseType;
        }

        foreach (Type t in types) {
            MemberInfo[] members = t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(x => x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property)
                .OrderBy(x => x.MetadataToken).ToArray();

            foreach (MemberInfo member in members) {
                /// Property replaces matching backing field
                if (member is PropertyInfo prop) {
                    string backingName = char.ToLower(prop.Name[0]) + prop.Name.Substring(1);
                    int backingIndex = result.FindIndex(x => x is FieldInfo f && f.Name == backingName);
                    if (0 <= backingIndex) {
                        result[backingIndex] = member;
                        continue;
                    }
                }

                int index = result.FindIndex(x => x.Name == member.Name && x.MemberType == member.MemberType);
                if (0 <= index) result[index] = member;
                else result.Add(member);
            }
        }

        return result;
    }

    public static object? DrawField (MemberInfo member, object? value) {
        if (member.GetCustomAttribute<Hide>() is not null) return null;

        string label = Utils.NameCapital(member.Name);

        DrawName? drawName = member.GetCustomAttribute<DrawName>();
        if (drawName is not null) label = drawName.Name;

        bool isReadonly = member.GetCustomAttribute<Readonly>() is not null;

        float step = valueStep;
        ChangeStep? changeSpeed = member.GetCustomAttribute<ChangeStep>();
        if (changeSpeed is not null) step = changeSpeed.Step;

        bool isRaw = false;
        if (member.GetCustomAttribute<Raw>() is not null) isRaw = true;

        return DrawField(label, value, isReadonly: isReadonly, step: step, isRaw: isRaw);
    }

    /// Core widget drawer, callable directly without reflection.
    /// Must be called while a 2-column ImGui table is open (see DrawObject).
    public static object? DrawField (string label, object? value, bool isReadonly = false, 
        float step = valueStep, bool isRaw = false) {
        object? result = null;

        if (isReadonly) ImGui.BeginDisabled(true);

        bool isRawCollection = isRaw && value is IDictionary;

        string id = label;
        if (drawInverted && !isRawCollection) {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding();
            ImGui.Text(label);
            ImGui.TableSetColumnIndex(1);
            ImGui.SetNextItemWidth(-1);
            id = "##" + label;
        }

        switch (value) {
            case int i:
                if (ImGui.DragInt(id, ref i)) result = i;
                break;
            case long l:
                int temp_i = (int)l;
                if (ImGui.DragInt(id, ref temp_i)) result = (long)temp_i;
                break;
            case float f:
                if (ImGui.DragFloat(id, ref f, step, 0, 0, "%.2f")) result = f;
                break;
            case double d:
                float temp_f = (float)d;
                if (ImGui.DragFloat(id, ref temp_f, step, 0, 0, "%.2f")) result = (double)temp_f;
                break;
            case bool b:
                if (ImGui.Checkbox(id, ref b)) result = b;
                break;
            case string s:
                if (ImGui.InputText(id, ref s, 256)) result = s;
                break;
            case Enum e:
                Array values = Enum.GetValues(e.GetType());
                string[] names = Enum.GetNames(e.GetType());
                int current = Array.IndexOf(values, e);
                if (ImGui.Combo(id, ref current, names, names.Length)) result = values.GetValue(current);
                break;
            case Guid g:
                string temp_s = g.ToString();
                if (ImGui.InputText(id, ref temp_s, 256)) result = temp_s;
                break;
            case Vector2 v2:
                if (ImGui.DragFloat2(id, ref v2, step, 0, 0, "%.2f")) result = v2;
                break;
            case Vector3 v3:
                if (ImGui.DragFloat3(id, ref v3, step, 0, 0, "%.2f")) result = v3;
                break;
            case Quaternion q:
                Vector4 temp_v4 = new Vector4(q.X, q.Y, q.Z, q.W);
                if (ImGui.DragFloat4(id, ref temp_v4, step, 0, 0, "%.2f"))
                    result = new Quaternion(temp_v4.X, temp_v4.Y, temp_v4.Z, temp_v4.W);
                break;
            case IDictionary dict:
                if (isRaw) {
                    foreach (object key in dict.Keys) {
                        object? entryValue = dict[key];
                        object? drawn = DrawField(key.ToString()!, entryValue, isReadonly: isReadonly, step: step, isRaw: isRaw);
                        if (drawn is not null) dict[key] = drawn;
                    }
                } else {
                    if (ImGui.TreeNodeEx(id, ImGuiTreeNodeFlags.DefaultOpen, label)) {
                        foreach (object key in dict.Keys) {
                            object? entryValue = dict[key];
                            object? drawn = DrawField(key.ToString()!, entryValue, isReadonly: isReadonly, step: step, isRaw: isRaw);
                            if (drawn is not null) dict[key] = drawn;
                        }
                        ImGui.TreePop();
                    }
                }
                break;

            case object o when o.GetType().IsGenericType && o.GetType().GetGenericTypeDefinition() == typeof(KeyValuePair<,>):
                object kvpKey = o.GetType().GetProperty("Key")!.GetValue(o)!;
                object? kvpVal = o.GetType().GetProperty("Value")!.GetValue(o);
                result = DrawField(kvpKey.ToString()!, kvpVal, isReadonly: isReadonly, step: step, isRaw: isRaw);
                break;

            case GameObject go:
                ImGui.BeginDisabled();
                temp_s = go.Name;
                if (ImGui.InputText(id, ref temp_s, 256)) result = temp_s;
                ImGui.EndDisabled();
                break;
            case Transform tr:
                ImGui.BeginDisabled();
                temp_s = tr.parent is not null ? tr.parent.Name : "null";
                if (ImGui.InputText(id, ref temp_s, 256)) result = temp_s;
                ImGui.EndDisabled();
                break;
            case Material mat:
                if (ImGui.TreeNodeEx(id, ImGuiTreeNodeFlags.DefaultOpen, label)) {
                    DrawObject(mat);
                    ImGui.TreePop();
                }
                break;
            case Mesh mesh:
                temp_s = mesh.Name;
                if (ImGui.InputText(id, ref temp_s, 256)) result = temp_s;
                break;
            case null:
                ImGui.BeginDisabled();
                string nullLabel = "null";
                ImGui.InputText(id, ref nullLabel, 256);
                ImGui.EndDisabled();
                break;
            default:
                ImGui.TextDisabled($"[fallback] {value}");
                break;
        }

        if (isReadonly) ImGui.EndDisabled();

        return result;
    }


    private void RegisterTypes () {
        List<MethodInfo> drawMethods = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsAbstract && t.IsSealed) /// static classes
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m => m.Name == "DrawInspector" && m.GetParameters().Length == 1)
            .Where(m => typeof(Component).IsAssignableFrom(m.GetParameters()[0].ParameterType))
            .ToList();

        foreach (var kv in ComponentManager.Instance.Components) {
            Type componentType = kv.Key;

            MethodInfo? best = drawMethods
                .Where(m => m.GetParameters()[0].ParameterType.IsAssignableFrom(componentType))
                .OrderByDescending(m => InheritanceDepth(m.GetParameters()[0].ParameterType))
                .FirstOrDefault();

            if (best is null) continue;

            MethodInfo method = best; /// capture per-iteration
            _drawers.Add(componentType, c => method.Invoke(null, new object[] { c }));

            //Log.log("EditorUI.ComponentRegister", componentType.Name, "->", method.DeclaringType?.Name);
        }
    }

    private static int InheritanceDepth (Type t) {
        int depth = 0;
        while (t != null) {
            depth++;
            t = t.BaseType;
        }
        return depth;
    }


    public void UpdateAvail () {
        if (_docked) sceneAvail = getSceneAvail();
    }
    public Vector2 getSceneAvail () {
        Vector2 avail = ImGui.GetContentRegionAvail();
        Vector2 _sceneAvail = new Vector2(MathF.Max(MathF.Floor(avail.X), 1), MathF.Max(MathF.Floor(avail.Y), 1));
        return _sceneAvail;
    }

    /// Mouse position remapped into Scene FBO pixel space, or null if outside the panel
    public void UpdateInput () {
        Inputs.MousePos_Scene = Inputs.MousePos - _sceneRectMin;

        if (!isSceneUIHovered) {
            Inputs.isMouseOverScene = false;
            return;
        }
        if (Inputs.MousePos_Scene.X < 0 || Inputs.MousePos_Scene.Y < 0 || 
            sceneAvail.X < Inputs.MousePos_Scene.X || sceneAvail.Y < Inputs.MousePos_Scene.Y) {
            Inputs.isMouseOverScene = false;
        }

        Inputs.isMouseOverScene = true;
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
