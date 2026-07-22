using System;
using System.Numerics;
using ImGuiNET;
using Silk.NET.OpenGL.Extensions.ImGui;
using Engine;
using Engine.Graphics;

namespace Engine.Graphics;


public class EditorUI : Singleton<EditorUI>, IDisposable {
    public EditorUI () {
        ImGUI = new ImGuiController(Renderer.Instance.GL, Engine.Window, Engine.Input);
        Renderer.Instance.de_Dispose += Dispose;

        ImGuiIOPtr io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

        ImGui.LoadIniSettingsFromDisk(io.IniFilename);
    }

    public readonly ImGuiController ImGUI = null!;
    public bool isUIClick = false;
    private bool _isClosing = false;


    public void Update () {
        if (_isClosing) return;

        ImGUI.Update((float)Engine.deltaTime);

        if (ImGui.GetIO().WantCaptureMouse || ImGui.IsAnyItemActive())
            isUIClick = true;
        else
            isUIClick = false;
    }

    public void Draw () {
        if (_isClosing) return;

        ImGuiViewportPtr viewport = ImGui.GetMainViewport();
        uint dockspaceId = ImGui.DockSpaceOverViewport(0, viewport, ImGuiDockNodeFlags.PassthruCentralNode);
        
        DrawInspector(dockspaceId);
        //DrawTool(dockspaceId);

        ImGUI.Render();

        ImGuiIOPtr io = ImGui.GetIO();
        if (io.WantSaveIniSettings) {
            ImGui.SaveIniSettingsToDisk(io.IniFilename);
            io.WantSaveIniSettings = false;
        }
    }

    private void DrawInspector (uint dockspaceId) {
        ImGui.SetNextWindowDockID(dockspaceId, ImGuiCond.FirstUseEver);

        ImGui.Begin("Inspector");
        GameObject? selectedGO = AssetsEngine._gizmo_Selected.selectedMesh?.owner;
        if (selectedGO is not null) {
            ImGui.Text("Selected:");
            ImGui.LabelText("Name", selectedGO.Name);
            ImGui.Text("Transform");
            ImGui.DragFloat3("Position", ref selectedGO.Transform.Position);
            ImGui.DragFloat3("Rotation", ref selectedGO.Transform.Rotation);
            ImGui.DragFloat3("Scale", ref selectedGO.Transform.Scale);
        }
        ImGui.End();
    }

    private void DrawTool (uint dockspaceId) {
        ImGui.SetNextWindowDockID(dockspaceId, ImGuiCond.FirstUseEver);

        ImGui.Begin("Tool");
        if (ImGui.Button("Reset")) {
            
        }
        ImGui.End();
    }



    public void Dispose () {
        if (_isClosing) return;
        _isClosing = true;

        ImGUI.Dispose();
    }

}
