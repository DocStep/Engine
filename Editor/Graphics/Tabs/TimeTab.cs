using ImGuiNET;

namespace Editor.Graphics;


public class TimeTab : IEditorTab {

    public string Name { get; set; } = "Time";
    public bool isActive { get; set; } = true;


    public void Draw () {
        ImGui.Begin(Name);

        EditorUI.DrawTabContext(this);

        EditorUI.DrawObject(typeof(Time));

        ImGui.End();
    }

}
