using ImGuiNET;

namespace Editor.Graphics;


public class HierarchyTab : IEditorTab {

    public string Name { get; set; } = "Hierarchy";
    public bool isActive { get; set; } = true;

    /// removal is deferred until after the draw loop so the collection isn't modified mid-iteration
    private GameObject? _toRemove = null;


    public void Draw () {
        ImGui.Begin(Name);

        EditorUI.DrawTabContext(this);

        Scene? scene = SceneManager.ActiveScene;
        if (scene is not null) {
            ImGui.PushStyleColor(ImGuiCol.Text, EditorUIStyle.AccentColor);
            ImGui.TextUnformatted(scene.Name);
            ImGui.PopStyleColor();
            ImGui.Separator();

            foreach (GameObject go in scene.GameObjects) {
                if (go.Transform.Parent is null) DrawHierarchyNode(go);
            }

            DrawAddGameObjectContext();

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

        bool open = ImGui.TreeNodeEx(go.Name, flags);

        if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen()) {
            /// set selection the same way the gizmo/inspector already reads it
            Gizmos._gizmo_Selected.UpdateSelected(go);
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
