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
using System.Runtime.CompilerServices;

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

    /// inspector row layout — Unity-style margin instead of an ImGui table
    private const float labelRatio = 0.4f;
    private const float minLabelWidth = 90f;


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
        DrawHierarchy(dockspaceId);
        DrawInspector(dockspaceId);
        DrawRendering(dockspaceId);
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

    private void DrawHierarchy (uint dockspaceId) {
        ImGui.Begin("Hierarchy");

        Scene scene = SceneManager.ActiveScene;
        ImGui.PushStyleColor(ImGuiCol.Text, EditorUIStyle.AccentColor);
        ImGui.TextUnformatted(scene.Name);
        ImGui.PopStyleColor();
        ImGui.Separator();

        foreach (GameObject go in scene.Objects) {
            if (go.Transform.Parent is null) DrawHierarchyNode(go);
        }

        ImGui.End();
    }
    private void DrawHierarchyNode (GameObject go) {
        ImGui.PushID(go.GetHashCode());

        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;

        if (go.Transform.Children.Count == 0)
            flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;

        GameObject? go_selected = Gizmos._gizmo_Selected.go_selected;
        if (go_selected == go) flags |= ImGuiTreeNodeFlags.Selected;

        bool open = ImGui.TreeNodeEx(go.Name, flags);

        if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen()) {
            /// set selection the same way the gizmo/inspector already reads it
            Gizmos._gizmo_Selected.UpdateSelected(go);
        }

        if (open && 0 < go.Transform.Children.Count) {
            for (int c = 0; c < go.Transform.Children.Count; c++) DrawHierarchyNode(go.Transform.Children[c].gameObject);
            ImGui.TreePop();
        }

        ImGui.PopID();
    }
    private void DrawInspector (uint dockspaceId) {
        ImGui.Begin("Inspector");

        GameObject? selectedGO = Gizmos._gizmo_Selected.go_selected;
        if (selectedGO is not null) {
            bool temp_b = selectedGO.Enabled;
            if (ImGui.Checkbox("##" + nameof(selectedGO.Enabled), ref temp_b)) selectedGO.Enabled = temp_b;
            ImGui.SameLine();
            ImGui.InputText(nameof(selectedGO.Name), ref selectedGO.Name, 256);
            ImGui.Separator();

            DrawComponent(selectedGO.Transform);

            for (int c = 0; c < selectedGO.Components.Count; c++) {
                DrawComponent(selectedGO.Components[c]);
            }
        }

        ImGui.End();
    }
    private void DrawRendering (uint dockspaceId) {
        ImGui.Begin("Rendering");

        DrawObject(typeof(Lighting));
        ImGui.Separator();
        DrawObject(Renderer.Instance.PostProcess.Effects, [new DrawName(nameof(Renderer.Instance.PostProcess.Effects))]);

        ImGui.End();
    }
    public void DrawComponent (Component component) {
        ImGui.PushID(component.GetHashCode());
        bool enabled = component.Enabled;
        if (ImGui.Checkbox("##" + nameof(component.Enabled), ref enabled)) {
            component.Enabled = enabled;
        }
        ImGui.SameLine();
        if (ImGui.CollapsingHeader(component.Name, ImGuiTreeNodeFlags.DefaultOpen)) {
            if (_drawers.TryGetValue(component.GetType(), out var componentDrawer)) {
                componentDrawer(component);
            } else {
                component.DrawInspector();
            }
        }
        ImGui.PopID();
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
        ImGui.Separator();
        DrawObject(Engine.Graphics.Shader.Stats);

        ImGui.EndDisabled();
        ImGui.End();
    }


    
    public static void DrawObject (object target, IEnumerable<Attribute>? attributes = null) {
        ImGui.PushID(target.GetHashCode());

        bool isCollection = target is IList or IDictionary
            && target is not (Vector2 or Vector3 or Vector4 or Quaternion);

        if (isCollection) {
            DrawLabel(target.GetType().FullName, target, attributes);
        } else {
            foreach (MemberInfo member in GetMembersInOrder(target.GetType()))
                DrawMember(target, member);
        }

        ImGui.PopID();
    }
    public static void DrawObject (Type staticType) {
        ImGui.PushID(staticType.GetHashCode());

        foreach (MemberInfo member in GetStaticMembersInOrder(staticType))
            DrawMember((object?)null, member);

        ImGui.PopID();
    }
    public static IEnumerable<MemberInfo> GetStaticMembersInOrder (Type type) {
        return type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(x => x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property)
            .OrderBy(x => x.MetadataToken);
    }



    private static void DrawMember (object? target, MemberInfo member) {
        object? value = member switch {
            FieldInfo f => f.GetValue(target),
            PropertyInfo p when p.CanRead => p.GetValue(target),
            _ => null,
        };

        bool isWritable = member switch {
            FieldInfo f => !f.IsLiteral && !f.IsInitOnly,
            PropertyInfo p => p.CanWrite,
            _ => false,
        };

        object? drawn = DrawMember(member, value);
        if (drawn is null || !isWritable) return;

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
            MemberInfo[] members = t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(x => x.MemberType == MemberTypes.Field
                    || (x is PropertyInfo p && x.MemberType == MemberTypes.Property && p.GetIndexParameters().Length == 0))
                .Where(x => !x.IsDefined(typeof(CompilerGeneratedAttribute), false))
                .OrderBy(x => x.MetadataToken).ToArray();

            foreach (MemberInfo member in members) {
                int index = result.FindIndex(x => x.Name == member.Name && x.MemberType == member.MemberType);
                if (0 <= index) result[index] = member;
                else result.Add(member);
            }
        }

        return result;
    }

    public static object? DrawMember (MemberInfo member, object? value) {
        return DrawLabel(member.Name, value, member?.GetCustomAttributes());
    }

    public static object? DrawLabel (string label, object? value, IEnumerable<Attribute>? attributes = null) {
        label = Utils.NameCapital((string)label);
        bool isReadonly = false;
        float step = valueStep;
        bool isRaw = false;

        if (attributes is not null) {
            Attribute[] attrs = attributes as Attribute[] ?? attributes.ToArray();

            if (attrs.OfType<Hide>().Any()) return null;

            DrawName? drawName = attrs.OfType<DrawName>().FirstOrDefault();
            if (drawName is not null) label = drawName.Name;

            isReadonly = attrs.OfType<Readonly>().Any();

            ChangeStep? changeSpeed = attrs.OfType<ChangeStep>().FirstOrDefault();
            if (changeSpeed is not null) step = changeSpeed.Step;

            isRaw = attrs.OfType<Raw>().Any();
        }

        if (isReadonly) ImGui.BeginDisabled(true);

        bool isCollection = value is (IList or IDictionary) && value is not (Vector2 or Vector3 or Vector4 or Quaternion);
        bool isNestedObject = value is Material or PostProcessPass;
        bool isRow = drawInverted && !isCollection && !isNestedObject;
        if (isRow) {
            float labelWidth = MathF.Max(ImGui.GetContentRegionAvail().X*labelRatio, minLabelWidth);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(label);
            ImGui.SameLine(labelWidth);
            ImGui.SetNextItemWidth(-1);
            label = "##" + label;
        }

        object? result = null;
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
            case Enum e:
                Array values = Enum.GetValues(e.GetType());
                string[] names = Enum.GetNames(e.GetType());
                int current = Array.IndexOf(values, e);
                if (ImGui.Combo(label, ref current, names, names.Length)) result = values.GetValue(current);
                break;
            case Guid g:
                string temp_s = g.ToString();
                if (ImGui.InputText(label, ref temp_s, 256)) result = temp_s;
                break;
            case Vector2 v2:
                if (ImGui.DragFloat2(label, ref v2, step, 0, 0, "%.2f")) result = v2;
                break;
            case Vector3 v3:
                if (ImGui.DragFloat3(label, ref v3, step, 0, 0, "%.2f")) result = v3;
                break;
            case Vector4 v4:
                if (ImGui.DragFloat4(label, ref v4, step, 0, 0, "%.2f")) result = v4;
                break;
            case Quaternion q:
                Vector4 temp_v4 = new Vector4(q.X, q.Y, q.Z, q.W);
                if (ImGui.DragFloat4(label, ref temp_v4, step, 0, 0, "%.2f"))
                    result = new Quaternion(temp_v4.X, temp_v4.Y, temp_v4.Z, temp_v4.W);
                break;
            case IList list when value is not (Vector2 or Vector3 or Vector4 or Quaternion):
                attributes = attributes?.Where(x => x.GetType() != typeof(DrawName));
                if (isRaw) {
                    for (int i = 0; i < list.Count; i++) {
                        object? entryValue = list[i];
                        object? drawn = DrawLabel($"{entryValue?.GetType().Name}[{i}]", entryValue, attributes);
                        if (drawn is not null) list[i] = drawn;
                    }
                } else {
                    if (ImGui.TreeNodeEx(label, ImGuiTreeNodeFlags.DefaultOpen, label)) {
                        for (int i = 0; i < list.Count; i++) {
                            object? entryValue = list[i];
                            object? drawn = DrawLabel($"{entryValue?.GetType().Name}[{i}]", entryValue, attributes);
                            if (drawn is not null) list[i] = drawn;
                        }
                        ImGui.TreePop();
                    }
                }
                break;
            case IDictionary dict:
                attributes = attributes?.Where(x => x.GetType() != typeof(DrawName));
                if (isRaw) {
                    foreach (object key in dict.Keys) {
                        object? entryValue = dict[key];
                        object? drawn = DrawLabel(key.ToString()!, entryValue, attributes);
                        if (drawn is not null) dict[key] = drawn;
                    }
                } else {
                    if (ImGui.TreeNodeEx(label, ImGuiTreeNodeFlags.DefaultOpen, label)) {
                        foreach (object key in dict.Keys) {
                            object? entryValue = dict[key];
                            object? drawn = DrawLabel(key.ToString()!, entryValue, attributes);
                            if (drawn is not null) dict[key] = drawn;
                        }
                        ImGui.TreePop();
                    }
                }
                break;

            case object o when o.GetType().IsGenericType && o.GetType().GetGenericTypeDefinition() == typeof(KeyValuePair<,>):
                object kvpKey = o.GetType().GetProperty("Key")!.GetValue(o)!;
                object? kvpVal = o.GetType().GetProperty("Value")!.GetValue(o);
                result = DrawLabel(kvpKey.ToString()!, kvpVal, attributes);
                break;

            case GameObject go:
                ImGui.BeginDisabled();
                temp_s = go.Name;
                if (ImGui.InputText(label, ref temp_s, 256)) result = temp_s;
                ImGui.EndDisabled();
                break;
            case Transform tr:
                ImGui.BeginDisabled();
                temp_s = tr.Parent is not null ? tr.Parent.Name : "null";
                if (ImGui.InputText(label, ref temp_s, 256)) result = temp_s;
                ImGui.EndDisabled();
                break;
            case Material mat:
                if (ImGui.TreeNodeEx(label, ImGuiTreeNodeFlags.DefaultOpen, label)) {
                    DrawObject(mat);
                    ImGui.TreePop();
                }
                break;
            case Mesh mesh:
                temp_s = mesh.Name;
                if (ImGui.InputText(label, ref temp_s, 256)) result = temp_s;
                break;
            case PostProcessPass ppp:
                if (ImGui.TreeNodeEx(label, ImGuiTreeNodeFlags.DefaultOpen, label)) {
                    DrawObject(ppp);
                    ImGui.TreePop();
                }
                break;

            case null:
                ImGui.BeginDisabled();
                string nullLabel = "null";
                ImGui.InputText(label, ref nullLabel, 256);
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
        Inputs.MousePos = Inputs.MousePos_Window - _sceneRectMin;

        if (!isSceneUIHovered) {
            Inputs.isMouseOverScene = false;
            return;
        }
        if (Inputs.MousePos.X < 0 || Inputs.MousePos.Y < 0 ||
            sceneAvail.X < Inputs.MousePos.X || sceneAvail.Y < Inputs.MousePos.Y) {
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