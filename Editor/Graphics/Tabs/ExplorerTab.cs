using ImGuiNET;

namespace Editor.Graphics;


public class ExplorerTab : IEditorTab {

    public string Name { get; set; } = "Explorer";
    public bool isActive { get; set; } = true;
    //static readonly string RootPath = Path.GetFullPath("src");

    //static string pathSelected = string.Empty;


    public void Draw () {
        ImGui.Begin("Explorer");
        DrawDirectory(Dirs.AssetsPath);
        ImGui.End();
    }

    void DrawDirectory (string path) {
        foreach (var dir in Directory.GetDirectories(path)) {
            var name = Path.GetFileName(dir);
            if (ImGui.TreeNode(name)) {
                DrawDirectory(dir);
                ImGui.TreePop();
            }
        }

        foreach (var file in Directory.GetFiles(path)) {
            var name = Path.GetFileName(file);
            if (ImGui.Selectable(name)) {
                OnFileSelected(file);
            }
        }
    }

    void OnFileSelected (string path) {
        /// hook up open/select logic here
        //pathSelected = path;
    }

}
