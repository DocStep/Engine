using ImGuiNET;

namespace Editor.Graphics;


public class HierarchyTab : IEditorTab {

    public string Name { get; set; } = "Hierarchy";
    public bool isActive { get; set; } = true;

    /// Hierarchy mutations are deferred until after traversal to avoid changing child lists mid-iteration.
    private GameObject? _toRemove = null;
    private GameObject? _toReparent = null;
    private Transform? _newParent;
    private bool _hasReparentRequest;
    private GameObject? _expandOnNextFrame;


    public void Draw () {
        ImGui.Begin(Name);

        EditorUI.DrawTabContext(this);

        Scene? scene = SceneManager.ActiveScene;
        if (scene is not null) {
            ImGui.PushStyleColor(ImGuiCol.Text, EditorUIStyle.AccentColor);
            string sceneName = scene.Name;
            ImGui.SetNextItemWidth(-1);
            bool sceneNameChanged = ImGui.InputText("##SceneName", ref sceneName, 256);
            if (sceneNameChanged) {
                scene.Name = sceneName;
                scene.MarkDirty();
            }
            if (ImGui.IsItemDeactivatedAfterEdit()) Project.RenameSceneFile(scene);
            ImGui.PopStyleColor();
            ImGui.Separator();
            ImGui.Button("Scene Root", new System.Numerics.Vector2(-1f, 0f));
            DrawRootDropTarget();

            foreach (GameObject go in scene.GameObjects) {
                if (go.Transform.Parent is null) DrawHierarchyNode(go);
            }

            DrawAddGameObjectContext();

            if (_hasReparentRequest && _toReparent is not null) {
                _toReparent.Transform.Parent = _newParent;
                _toReparent = null;
                _newParent = null;
                _hasReparentRequest = false;
            }

            if (_toRemove is not null) {
                if (Gizmos._gizmo_Selected.go_selected == _toRemove) Gizmos._gizmo_Selected.UpdateSelected(null); /// needs a nullable param
                _toRemove.Destroy(); /// adjust to your API
                _toRemove = null;
            }
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
        if (_expandOnNextFrame == go) {
            ImGui.SetNextItemOpen(true);
            _expandOnNextFrame = null;
        }

        bool open = ImGui.TreeNodeEx(go.Name, flags);

        if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen()) {
            Gizmos._gizmo_Selected.UpdateSelected(go);
        }
        if (ImGui.BeginDragDropSource()) {
            EditorUI.SetGameObjectDragPayload(go);
            ImGui.TextUnformatted(go.Name);
            ImGui.EndDragDropSource();
        }
        if (ImGui.BeginDragDropTarget()) {
            GameObject? dragged = EditorUI.AcceptGameObjectDragPayload();
            if (dragged is not null && CanSetParent(dragged, go)) {
                QueueReparent(dragged, go.Transform);
                _expandOnNextFrame = go;
            }
            ImGui.EndDragDropTarget();
        }

        /// must come right after the tree node, it attaches to the last item
        if (ImGui.BeginPopupContextItem("##NodeContext")) {
            /// RMB also selects, like Unity
            Gizmos._gizmo_Selected.UpdateSelected(go);

            if (ImGui.MenuItem("Remove")) _toRemove = go;

            ImGui.EndPopup();
        }

        if (open && 0 < go.Transform.Children.Count) {
            for (int c = 0; c < go.Transform.Children.Count; c++) DrawHierarchyNode(go.Transform.Children[c].gameObject);
            ImGui.TreePop();
        }

        ImGui.PopID();
    }
    private void DrawRootDropTarget () {
        if (!ImGui.BeginDragDropTarget()) return;
        GameObject? dragged = EditorUI.AcceptGameObjectDragPayload();
        if (dragged is not null) QueueReparent(dragged, null);
        ImGui.EndDragDropTarget();
    }
    private void QueueReparent (GameObject gameObject, Transform? parent) {
        _toReparent = gameObject;
        _newParent = parent;
        _hasReparentRequest = true;
    }
    private static bool CanSetParent (GameObject child, GameObject parent) {
        if (child == parent) return false;
        for (Transform? ancestor = parent.Transform; ancestor is not null; ancestor = ancestor.Parent)
            if (ancestor == child.Transform) return false;
        return true;
    }

    public static void DrawAddGameObjectContext () {
        if (!ImGui.BeginPopupContextWindow("##HierarchyContext", ImGuiPopupFlags.MouseButtonRight | ImGuiPopupFlags.NoOpenOverItems))
            return;

        if (ImGui.MenuItem("Empty")) {
            Gizmos._gizmo_Selected.UpdateSelected(new GameObject());
        }

        if (ImGui.BeginMenu("Primitive")) {
            foreach (PrimitiveTypes primitive in Enum.GetValues<PrimitiveTypes>()) {
                if (ImGui.MenuItem(primitive.ToString())) {
                    GameObject go = new GameObject(primitive, Vector3.Zero, Vector3.Zero, Vector3.One, primitive.ToString());
                    Gizmos._gizmo_Selected.UpdateSelected(go);
                }
            }
            ImGui.EndMenu();
        }

        ImGui.EndPopup();
    }

}
