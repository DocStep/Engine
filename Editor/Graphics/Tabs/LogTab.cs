using ImGuiNET;

namespace Editor.Graphics;


public class LogTab : IEditorTab {

    public string Name { get; set; } = "Log";
    public bool isActive { get; set; } = true;


    public void Draw () {
        ImGui.Begin(Name);
        ImGui.BeginDisabled();

        EditorUI.DrawObject(typeof(Log));

        ImGui.EndDisabled();
        ImGui.End();
    }

}
