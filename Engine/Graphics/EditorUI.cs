using Silk.NET.OpenGL.Extensions.ImGui;
using ImGuiNET;

namespace Engine.Graphics;


public class EditorUI : Singleton<EditorUI>, IDisposable {
    public EditorUI () {
        ImGUI = new ImGuiController(Renderer.Instance.GL, Engine.Window, Engine.Input);
        Renderer.Instance.de_Dispose += Dispose;
    }

    public readonly ImGuiController ImGUI = null!;
    public bool isUIClick = false;
    private bool _isClosing = false;


    public void Update () {
        ImGUI.Update((float)Engine.deltaTime);

        if (!_isClosing && ImGui.GetIO().WantCaptureMouse)
            isUIClick = true;
        else isUIClick = false;
    }

    public void Draw () {
        ImGui.Begin("Inspector");
        GameObject? selectedGO = Renderer.Instance._gizmo_Selected.selectedMesh?.owner;
        if (selectedGO is not null) {
            ImGui.Text("Selected: " + Renderer.Instance._gizmo_Selected.selectedMesh?.owner.Name);
            ImGui.DragFloat3("Position", ref selectedGO.Transform.Position);
        }
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        ImGui.End();

        ImGUI.Render();
    }


    public void Dispose () {
        ImGUI.Dispose();
        _isClosing = true;
    }

}
