using System;
using System.Numerics;
using System.Linq;
using System.Reflection;
using Marshal = System.Runtime.InteropServices.Marshal;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using ImGuiNET;
using Engine;
using Editor;
using Engine.Graphics;
using Engine.Input;

namespace Editor.Graphics;


public class EditorUI : Singleton<EditorUI>, IDisposable {

    protected override void Init () {
        ImGUI = new ImGuiController(Renderer.GL, Windows.Window, Windows.Input);

        SetDock();
        EditorUIStyle.SetAccentColor();
        //SetFont(AssetsEngine._fontData);

        ImGui.LoadIniSettingsFromDisk(ImGui.GetIO().IniFilename);

        EditorTabs.RegisterTabs();
        EditorTabs.LoadTabs();

        Inputs.de_UpdateInput += UpdateInput;

        Engine.Engine.Instance.de_UpdateAlways += Update;
        Engine.Engine.Instance.de_Render += Draw;
        Engine.Engine.Instance.de_Closing += EditorTabs.Closing;

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
    private const float toolbarHeight = 20f;


    private Vector2 sceneAvail = new Vector2(1280, 720);
    public Vector2 SceneAvail => sceneAvail;

    public Vector2 _sceneRectMin { get; private set; }
    public Vector2 _sceneRectMax { get; private set; }
    public void UpdateSceneRect (Vector2 min, Vector2 max) {
        _sceneRectMin = min;
        _sceneRectMax = max;
    }

    public bool isSceneUIHovered { get; private set; }
    public void UpdateUIHovered (bool isHovered) {
        isSceneUIHovered = isHovered;
    }
    public bool isMouseHooked () {
        Vector2 availSize = ImGui.GetContentRegionAvail();
        Vector2 elementPos = ImGui.GetCursorScreenPos();
        return true;
    }


    public void Update () {
        if (_isClosing) return;

        ImGUI.Update((float)Time.unscaledDeltaTime);

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

        DrawToolbar();

        uint dockspaceId = ImGui.GetID("MainDockspace");
        DrawDockHost(dockspaceId);

        ImGui.SetNextWindowDockID(dockspaceId, ImGuiCond.FirstUseEver);

        EditorTabs.Draw(dockspaceId);

        //ImGui.ShowMetricsWindow();

        ImGUI.Render();

        ImGuiIOPtr io = ImGui.GetIO();
        if (io.WantSaveIniSettings) {
            ImGui.SaveIniSettingsToDisk(io.IniFilename);
            io.WantSaveIniSettings = false;
        }

        _docked = true;
    }


    private void DrawToolbar () {
        ImGuiViewportPtr viewport = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(viewport.Pos);
        ImGui.SetNextWindowSize(new Vector2(viewport.Size.X, toolbarHeight));
        ImGui.SetNextWindowSizeConstraints(
            new Vector2(viewport.Size.X, toolbarHeight),
            new Vector2(viewport.Size.X, toolbarHeight)
        );
        ImGui.SetNextWindowViewport(viewport.ID);

        ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoBringToFrontOnFocus;
        flags |= ImGuiWindowFlags.AlwaysAutoResize;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4, 0));
        //ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, 0));
        //ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);

        ImGui.Begin("##Toolbar", flags);

        if (ImGui.Button("Tabs")) {
            ImGui.OpenPopup("##TabsContext");
        }

        EditorTabs.ContextDrawTab();

        //ImGui.SameLine();
        //ImGui.Separator();
        //ImGui.SameLine();

        //string playName = "Play";
        //float buttonWidth = ImGui.CalcTextSize(playName).X + ImGui.GetStyle().FramePadding.X * 2;
        //ImGui.SetCursorPosX((ImGui.GetWindowWidth() - buttonWidth) * 0.5f);
        //if (ImGui.Button(playName)) { }

        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    private void DrawDockHost (uint dockspaceId) {
        ImGuiViewportPtr viewport = ImGui.GetMainViewport();
        Vector2 hostPos = viewport.Pos + new Vector2(0, toolbarHeight);
        Vector2 hostSize = viewport.Size - new Vector2(0, toolbarHeight);

        ImGui.SetNextWindowPos(hostPos);
        ImGui.SetNextWindowSize(hostSize);
        ImGui.SetNextWindowViewport(viewport.ID);

        ImGuiWindowFlags hostFlags =
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus |
            ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDocking;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        ImGui.Begin("##DockHost", hostFlags);
        ImGui.PopStyleVar(3);

        ImGui.DockSpace(dockspaceId, Vector2.Zero, ImGuiDockNodeFlags.PassthruCentralNode);

        ImGui.End();
    }

    public static void DrawTabContext (IEditorTab tab) {
        //if (ImGui.BeginPopupContextItem("##" + Name + "TabContext")) {
        if (ImGui.BeginPopupContextItem("##TabContext")) {
            if (ImGui.MenuItem("Close"))
                tab.isActive = false;

            ImGui.EndPopup();
        }
    }
    
    public static void DrawComponent (Component component) {
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



    
    public static void DrawObject (object target, IEnumerable<Attribute>? attributes = null) {
        ImGui.PushID(target.GetHashCode());

        bool isCollection = target is IList or IDictionary
            && target is not (Vector2 or Vector3 or Vector4 or Quaternion);

        if (isCollection) {
            string? type = target.GetType().FullName;
            if (type is not null) 
                DrawLabel(type, target, attributes);
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

        IEnumerable<Attribute> attrs = member.GetCustomAttributes();
        //if (!isWritable) attrs = attrs.Append(new Readonly());

        object? drawn = DrawMember(member, value, attrs);
        if (drawn is null || !isWritable) return;

        switch (member) {
            case FieldInfo f: f.SetValue(target, drawn); break;
            case PropertyInfo p when p.CanWrite: p.SetValue(target, drawn); break;
        }
    }

    public static IEnumerable<MemberInfo> GetMembersInOrder (Type type) {
        List<MemberInfo> result = new();
        List<Type> types = new();

        while (type is not null && type != typeof(object)) {
            types.Insert(0, type);
            type = type.BaseType;
        }

        foreach (Type t in types) {
            MemberInfo[] members = t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(x => x.MemberType == MemberTypes.Field
                    || (x is PropertyInfo p && x.MemberType == MemberTypes.Property && p.GetIndexParameters().Length == 0))
                .Where(x => !x.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false))
                .OrderBy(x => x.MetadataToken).ToArray();

            foreach (MemberInfo member in members) {
                int index = result.FindIndex(x => x.Name == member.Name && x.MemberType == member.MemberType);
                if (0 <= index) result[index] = member;
                else result.Add(member);
            }
        }

        return result;
    }

    public static object? DrawMember (MemberInfo member, object? value, IEnumerable<Attribute>? attributes = null) {
        return DrawLabel(member.Name, value, attributes);
    }

    public static void DrawVar<T> (string label, ref T value, IEnumerable<Attribute>? attributes = null) {
        object? result = DrawLabel(label, value, attributes);
        if (result is not null) value = (T)result;
    }
    public static object? DrawLabel (string label, object? value, IEnumerable<Attribute>? attributes = null) {
        //label = Utils.NameCapital(label);
        bool isReadonly = false;
        float step = valueStep;
        bool isRaw = false;
        bool isColor = false;

        if (attributes is not null) {
            Attribute[] attrs = attributes as Attribute[] ?? attributes.ToArray();

            if (attrs.OfType<Hide>().Any()) return null;

            InspectorName? drawName = attrs.OfType<InspectorName>().FirstOrDefault();
            if (drawName is not null) label = drawName.Name;

            isReadonly = attrs.OfType<Readonly>().Any();

            ChangeStep? changeSpeed = attrs.OfType<ChangeStep>().FirstOrDefault();
            if (changeSpeed is not null) step = changeSpeed.Step;

            isRaw = attrs.OfType<Raw>().Any();
            isColor = attrs.OfType<DrawColor>().Any();
        }

        if (isReadonly) ImGui.BeginDisabled(true);

        bool isCollection = (value is IList or IDictionary || (value?.GetType().IsGenericType == true &&
            value.GetType().GetGenericTypeDefinition() == typeof(System.Collections.Concurrent.ConcurrentQueue<>)))
            && value is not (Vector2 or Vector3 or Vector4 or Quaternion);
        bool isNestedObject = value is GameObject or Component or Material or PostProcessPass or LogEntry;
        bool isRow = drawInverted && !isCollection && !isNestedObject;
        if (isRow) {
            InvertedOrder(ref label);
        }

        object? result = null;
        switch (value) {
            case int i:
                if (ImGui.DragInt(label, ref i)) result = i;
                break;
            case uint ui:
                if (DragUInt(label, ref ui)) result = ui;
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
                if (ImGui.DragFloat(label, ref temp_f, step, 0, 0, "%.4f")) result = (double)temp_f;
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
                if (isColor) {
                    Vector3 rg = new(v2.X, v2.Y, 0);
                    if (ImGui.ColorEdit3(label, ref rg)) result = new Vector2(rg.X, rg.Y);
                } else {
                    if (ImGui.DragFloat2(label, ref v2, step, 0, 0, "%.2f")) result = v2;
                }
                break;
            case Vector3 v3:
                if (isColor) {
                    if (ImGui.ColorEdit3(label, ref v3)) result = v3;
                    //if (ImGui.ColorPicker3(label, ref v3)) result = v3;
                } else {
                    if (ImGui.DragFloat3(label, ref v3, step, 0, 0, "%.2f")) result = v3;
                }
                break;
            case Vector4 v4:
                if (isColor) {
                    if (ImGui.ColorEdit4(label, ref v4)) result = v4;
                    //if (ImGui.ColorPicker4(label, ref v4)) result = v4;
                } else {
                    if (ImGui.DragFloat4(label, ref v4, step, 0, 0, "%.2f")) result = v4;
                }
                break;
            case Quaternion q:
                Vector4 temp_v4 = new Vector4(q.X, q.Y, q.Z, q.W);
                if (ImGui.DragFloat4(label, ref temp_v4, step, 0, 0, "%.2f"))
                    result = new Quaternion(temp_v4.X, temp_v4.Y, temp_v4.Z, temp_v4.W);
                break;
            case IList list when value is not (Vector2 or Vector3 or Vector4 or Quaternion):
                attributes = attributes?.Where(x => x.GetType() != typeof(InspectorName));
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
                attributes = attributes?.Where(x => x.GetType() != typeof(InspectorName));
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
            case object o when o.GetType().IsGenericType && o.GetType().GetGenericTypeDefinition() == typeof(System.Collections.Concurrent.ConcurrentQueue<>):
                attributes = attributes?.Where(x => x.GetType() != typeof(InspectorName));
                IEnumerable queueEnumerable = (IEnumerable)o;
                object[] entries = queueEnumerable.Cast<object>().ToArray();
                ImGui.BeginDisabled(true);
                if (isRaw) {
                    for (int i = 0; i < entries.Length; i++) {
                        DrawLabel($"##{entries[i]?.GetType().Name}[{i}]", entries[i], attributes);
                    }
                } else {
                    if (ImGui.TreeNodeEx(label, ImGuiTreeNodeFlags.DefaultOpen, label)) {
                        for (int i = 0; i < entries.Length; i++) {
                            //DrawLabel($"##{entries[i]?.GetType().Name}[{i}]", entries[i], attributes);
                            DrawLabel("##", entries[i], attributes);
                        }
                        ImGui.TreePop();
                    }
                }
                ImGui.EndDisabled();
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
            case LogEntry log:
                ImGui.BeginDisabled();
                if (ImGui.InputText(label, ref log.text, 256)) result = log.text;
                ImGui.EndDisabled();
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
    private static unsafe bool DragUInt (string label, ref uint value, float speed = 1f, uint min = 0, uint max = 0) {
        fixed (uint* p = &value) {
            return ImGui.DragScalar(label, ImGuiDataType.U32, (IntPtr)p, speed,
                (IntPtr)(&min), (IntPtr)(&max));
        }
    }


    public static void InvertedOrder (ref string label) {
        float labelWidth = MathF.Max(ImGui.GetContentRegionAvail().X*labelRatio, minLabelWidth);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);
        ImGui.SameLine(labelWidth);
        ImGui.SetNextItemWidth(-1);
        label = "##" + label;
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


    public Vector2 UpdateAvail () {
        if (_docked) sceneAvail = getSceneAvail();
        return sceneAvail;
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
            Inputs.isMouseOver = false;
            return;
        }
        if (Inputs.MousePos.X < 0 || Inputs.MousePos.Y < 0 ||
            sceneAvail.X < Inputs.MousePos.X || sceneAvail.Y < Inputs.MousePos.Y) {
            Inputs.isMouseOver = false;
        }

        Inputs.isMouseOver = true;
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