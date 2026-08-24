using ImGuiNET;

namespace Editor.Graphics;


public class RenderingTab : IEditorTab {

    public string Name { get; set; } = "Rendering";
    public bool isActive { get; set; } = true;


    public void Draw () {
        ImGui.Begin(Name);

        EditorUI.DrawTabContext(this);

        EditorUI.DrawObject(typeof(Engine.Graphics.Lighting));
        ImGui.Separator();
        EditorUI.DrawObject(Engine.Graphics.Renderer.Instance.PostProcess, 
            [new InspectorName(nameof(Engine.Graphics.Renderer.Instance.PostProcess.Effects))]);

        ImGui.End();
    }

}
