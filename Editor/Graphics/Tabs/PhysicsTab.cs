using ImGuiNET;

namespace Editor.Graphics;


public class PhysicsTab : IEditorTab {

    public string Name { get; set; } = "Physics";
    public bool isActive { get; set; } = true;


    public void Draw () {
        ImGui.Begin(Name);

        EditorUI.DrawTabContext(this);

        EditorUI.DrawObject(typeof(Physics));

        ImGui.End();
    }

}
