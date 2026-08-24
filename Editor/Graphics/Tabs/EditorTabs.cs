using System.Reflection;
using System.Linq;
using ImGuiNET;

namespace Editor.Graphics;


public static class EditorTabs {

    //private static List<IInspectorTab> tabs = new List<IInspectorTab>();
    private static Dictionary<string, IEditorTab> tabs = new Dictionary<string, IEditorTab>();
    //public static List<IInspectorTab> Tabs => tabs;

    private static string TabsFile => Path.Combine(
        AppContext.BaseDirectory,
        "RditorTabs.ini"
    );


    public static void RegisterTabs () {
        IEnumerable<Type> tabTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => typeof(IEditorTab).IsAssignableFrom(x)
                && !x.IsInterface
                && !x.IsAbstract);

        foreach (Type type in tabTypes) {
            if (Activator.CreateInstance(type) is IEditorTab tab)
                RegisterTab(tab);
        }
    }
    public static void RegisterTab (IEditorTab tab) {
        tabs.Add(tab.Name, tab);
    }


    public static void ContextDrawTab () {
        if (!ImGui.BeginPopup("##" + "TabsContext")) return;

        foreach (var tabKV in tabs) {
            IEditorTab tab = tabKV.Value;
            bool active = tab.isActive;

            if (ImGui.Checkbox(tab.Name, ref active))
                tab.isActive = active;
        }

        ImGui.EndPopup();
    }


    public static void Open (string name) {
        if (tabs.TryGetValue(name, out IEditorTab? tab)) {
            if (!tab.isActive) 
                tab.isActive = true;
        }
    }

    public static void Draw (uint dockspaceId) {
        foreach (var tabKV in tabs) {
            if (tabKV.Value.isActive) {
                IEditorTab tab = tabKV.Value;
                tab.Draw();
            }
                
        }
    }


    public static void SaveTabs () {
        using StreamWriter writer = new(TabsFile);

        foreach (var tabKV in tabs) {
            writer.WriteLine($"{tabKV.Key}|{tabKV.Value.isActive}");
        }
    }
    public static void LoadTabs () {
        if (!File.Exists(TabsFile)) return;

        foreach (string line in File.ReadAllLines(TabsFile)) {
            string[] split = line.Split('|', 2);
            if (split.Length != 2) continue;
            
            if (tabs.TryGetValue(split[0], out IEditorTab? tab) &&
                bool.TryParse(split[1], out bool active)) {
                tab.isActive = active;
            }
        }
    }

    public static void Closing () {
        SaveTabs();
    }

}
