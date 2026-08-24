using ImGuiNET;

namespace Editor.Graphics;


public class GeneralTab : IEditorTab {

    public string Name { get; set; } = "General";
    public bool isActive { get; set; } = true;


    public void Draw () {
        ImGui.Begin(Name);

        EditorUI.DrawTabContext(this);

        //DrawVar(nameof(Time.timeScale), ref Time.timeScale);

        ImGui.End();
    }

}
