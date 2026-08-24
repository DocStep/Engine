using ImGuiNET;

namespace Editor.Graphics;


public class InspectorTab : IEditorTab {

    public string Name { get; set; } = "Inspector";
    public bool isActive { get; set; } = true;


    public void Draw () {
        ImGui.Begin(Name);

        EditorUI.DrawTabContext(this);

        GameObject? selectedGO = Gizmos._gizmo_Selected.go_selected;
        if (selectedGO is not null) {
            bool temp_b = selectedGO.Enabled;
            if (ImGui.Checkbox("##" + nameof(selectedGO.Enabled), ref temp_b)) selectedGO.Enabled = temp_b;
            ImGui.SameLine();
            ImGui.InputText(nameof(selectedGO.Name), ref selectedGO.Name, 256);
            ImGui.Separator();

            EditorUI.DrawComponent(selectedGO.Transform);

            for (int c = 0; c < selectedGO.Components.Count; c++) {
                EditorUI.DrawComponent(selectedGO.Components[c]);
            }
        }

        ImGui.End();
    }

}
