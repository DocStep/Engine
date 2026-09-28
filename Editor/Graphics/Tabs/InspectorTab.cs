using System.Linq;
using System.Reflection;
using ImGuiNET;

namespace Editor.Graphics;


public class InspectorTab : IEditorTab {

    public string Name { get; set; } = "Inspector";
    public bool isActive { get; set; } = true;

    private static List<Type>? _addableComponents;
    private static string _componentFilter = "";


    public void Draw () {
        ImGui.Begin(Name);

        EditorUI.DrawTabContext(this);

        GameObject? selectedGO = Gizmos._gizmo_Selected.go_selected;
        if (selectedGO is not null) {
            bool temp_b = selectedGO.Enabled;
            if (ImGui.Checkbox("##" + nameof(selectedGO.Enabled), ref temp_b)) selectedGO.Enabled = temp_b;
            ImGui.SameLine();
            string name = selectedGO.Name;
            ImGui.InputText(nameof(selectedGO.Name), ref name, 256);
            selectedGO.Name = name;
            ImGui.Separator();

            EditorUI.DrawComponent(selectedGO.Transform); /// Transform's menu item is disabled

            /// removal is deferred until after the loop so the list isn't modified mid-iteration
            Component? toRemove = null;
            for (int c = 0; c < selectedGO.Components.Count; c++) {
                if (EditorUI.DrawComponent(selectedGO.Components[c])) toRemove = selectedGO.Components[c];
            }
            if (toRemove is not null) selectedGO.RemoveComponent(toRemove); /// adjust to your API

            DrawAddComponentButton(selectedGO);
        }

        ImGui.End();
    }

    public static void DrawAddComponentButton (GameObject go) {
        ImGui.Separator();
        if (ImGui.Button("Add Component", new Vector2(-1, 0))) {
            _componentFilter = "";
            ImGui.OpenPopup("##AddComponent");
        }

        if (!ImGui.BeginPopup("##AddComponent")) return;

        ImGui.SetNextItemWidth(220);
        ImGui.InputTextWithHint("##filter", "Search...", ref _componentFilter, 64);

        if (ImGui.BeginChild("##list", new Vector2(220, 240))) {
            foreach (Type type in GetAddableComponents()) {
                if (_componentFilter.Length > 0 && !type.Name.Contains(_componentFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (ImGui.Selectable(type.Name)) {
                    go.AddComponent(type); /// adjust to your API
                    ImGui.CloseCurrentPopup();
                }
            }
        }
        ImGui.EndChild();

        ImGui.EndPopup();
    }

    /// every non-abstract Component subclass, cached
    private static List<Type> GetAddableComponents () {
        _addableComponents ??= AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(LoadTypes)
            .Where(t => t.IsClass && !t.IsAbstract && typeof(Component).IsAssignableFrom(t))
            .OrderBy(t => t.Name)
            .ToList();

        return _addableComponents;
    }

    private static IEnumerable<Type> LoadTypes (Assembly assembly) {
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException e) {
            return e.Types.Where(t => t is not null)!;
        }
    }

}
