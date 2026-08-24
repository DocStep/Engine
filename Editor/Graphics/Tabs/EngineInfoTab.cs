using ImGuiNET;

namespace Editor.Graphics;


public class EngineInfoTab : IEditorTab {

    public string Name { get; set; } = "Engine Info";
    public bool isActive { get; set; } = true;


    public void Draw () {
        ImGui.Begin(Name);
        ImGui.BeginDisabled();

        EditorUI.DrawTabContext(this);

        EditorUI.DrawObject(Engine.Engine.Instance.Stats);

        ImGui.EndDisabled();
        ImGui.End();
    }

}
