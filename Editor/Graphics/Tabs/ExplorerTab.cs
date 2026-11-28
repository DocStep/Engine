using ImGuiNET;

namespace Editor.Graphics;


public class ExplorerTab : IEditorTab {

    public string Name { get; set; } = "Explorer";
    public bool isActive { get; set; } = true;
    //static readonly string RootPath = Path.GetFullPath("src");

    //static string pathSelected = string.Empty;


    public void Draw () {
        ImGui.Begin("Explorer");

        EditorUI.DrawTabContext(this);

        DrawDirectory(Dirs.AssetsPath);
        if (ImGui.BeginPopupContextWindow("##ExplorerCreateContext", ImGuiPopupFlags.MouseButtonRight | ImGuiPopupFlags.NoOpenOverItems)) {
            DrawCreateContext(Dirs.AssetsPath);
            ImGui.EndPopup();
        }

        if (!string.Equals(Path.GetFullPath(Dirs.Scenes), Path.GetFullPath(Path.Combine(Dirs.AssetsPath, "Scenes")), StringComparison.OrdinalIgnoreCase)) {
            bool scenesOpen = ImGui.TreeNode("Project Scenes");
            if (ImGui.BeginPopupContextItem("##ProjectScenesContext")) {
                DrawCreateContext(Dirs.Scenes);
                ImGui.EndPopup();
            }
            if (scenesOpen) {
                DrawDirectory(Dirs.Scenes);
                ImGui.TreePop();
            }
        }

        ImGui.End();
    }

    void DrawDirectory (string path) {
        foreach (var dir in Directory.GetDirectories(path)) {
            var name = Path.GetFileName(dir);
            bool open = ImGui.TreeNode(name);
            if (ImGui.BeginPopupContextItem($"##FolderContext_{dir}")) {
                DrawCreateContext(dir);
                ImGui.EndPopup();
            }
            if (open) {
                DrawDirectory(dir);
                ImGui.TreePop();
            }
        }

        foreach (var file in Directory.GetFiles(path)) {
            var name = Path.GetFileName(file);
            bool selected = ImGui.Selectable(name);
            if (selected) OnFileSelected(file);

            if (ImGui.BeginDragDropSource()) {
                EditorUI.SetAssetDragPayload(file);
                ImGui.TextUnformatted(name);
                ImGui.EndDragDropSource();
            }

            if (ImGui.BeginPopupContextItem($"##FileContext_{file}")) {
                if (IsSceneFile(file) && ImGui.MenuItem("Open")) OpenScene(file);
                ImGui.EndPopup();
            }

            if (IsSceneFile(file)
                && ImGui.IsItemHovered()
                && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) {
                OpenScene(file);
            }
        }
    }

    static void DrawCreateContext (string directory) {
        if (ImGui.BeginMenu("Create")) {
            if (ImGui.MenuItem("Scene")) CreateScene(directory);
            ImGui.EndMenu();
        }
    }

    static void CreateScene (string directory) {
        Dirs.EnsureExist(directory);
        string path = GetUniqueScenePath(directory);
        Engine.Scene.Create(path);
    }

    static string GetUniqueScenePath (string directory) {
        string path = Path.Combine(directory, "New Scene.json");
        int suffix = 1;
        while (File.Exists(path)) {
            path = Path.Combine(directory, $"New Scene {suffix++}.json");
        }
        return path;
    }

    static bool IsSceneFile (string path) {
        return string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase);
    }

    static void OpenScene (string path) {
        Engine.Scene.Load(Path.GetFullPath(path));
    }

    void OnFileSelected (string path) {
        /// hook up open/select logic here
        //pathSelected = path;
    }

}
