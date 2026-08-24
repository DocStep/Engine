using ImGuiNET;

namespace Editor.Graphics;


public class SceneTab : IEditorTab {

    public string Name { get; set; } = "Scene";
    public bool isActive { get; set; } = true;


    public void Draw () {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.Begin(Name);

        Vector2 sceneAvail = EditorUI.Instance.UpdateAvail();
        ImGui.Image((IntPtr)Engine.Graphics.Renderer.Instance.PostProcess.OutputTexture, sceneAvail, new Vector2(0, 1), new Vector2(1, 0));

        EditorUI.Instance.UpdateSceneRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
        EditorUI.Instance.UpdateUIHovered(ImGui.IsItemHovered());

        if (!Engine.Input.Inputs.isMouseVisible) {
            Vector2 mousePos_Scene = Engine.Input.WindowInput.Mouse!.Position - ImGui.GetItemRectMin();
            float deltaX = MathF.Floor(mousePos_Scene.X/sceneAvail.X)*sceneAvail.X;
            float deltaY = MathF.Floor(mousePos_Scene.Y/sceneAvail.Y)*sceneAvail.Y;
            Vector2 delta = new Vector2(deltaX, deltaY);
            if (0 < delta.LengthSquared()) {
                Engine.Input.WindowInput.TeleportMouseDelta(-delta);
                //Log.log(sceneAvail, mousePos_Scene, isSceneUIHovered, delta);
            }
        }

        //Log.log("sceneAvail", sceneAvail, "mousePos_Scene", mousePos_Scene, "isUIHovered", isSceneUIHovered);
        //Log.log("mousePos_Scene", mousePos_Scene);

        ImGui.End();
        ImGui.PopStyleVar();
    }

}
