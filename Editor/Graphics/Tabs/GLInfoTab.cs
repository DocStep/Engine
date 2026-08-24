using ImGuiNET;

namespace Editor.Graphics;


public class GLInfoTab : IEditorTab {

    public string Name { get; set; } = "Renderer Info";
    public bool isActive { get; set; } = true;


    public void Draw () {
        ImGui.Begin(Name);
        ImGui.BeginDisabled();

        EditorUI.DrawObject(Engine.Graphics.Renderer.Instance.Stats);
        ImGui.Separator();
        EditorUI.DrawObject(Engine.Graphics.Shader.Stats);

        ImGui.EndDisabled();
        ImGui.End();
    }

}
