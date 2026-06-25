using System.Collections.Generic;

namespace Engine;


public static class ComponentManager {

    private readonly static Dictionary<Type, List<Component>> components =  new Dictionary<Type, List<Component>>();
    private readonly static List<IComponentUpdate> componentsUpdate = new List<IComponentUpdate>();
    private readonly static List<IComponentFixedUpdate> componentsFixedUpdate = new List<IComponentFixedUpdate>();
    public static int componentsCount => components.Count;
    /*public static string NameAll {
        get {
            string names = "All\n";
            foreach (var component in components) {
                names += " " + component.Key.Name;
            }
            return names;
        }
    }*/

    private readonly static List<IComponentUpdate> componentsRender = new List<IComponentUpdate>();


    public static void Init () {
        Type[] types = Reflection.FindAllSubclasses<Component>();
        for (int t = 0; t < types.Length; t++) {
            components.Add(types[t], new());
        }
    }

    internal static void FixedUpdate () {
        for (int c = 0; c < componentsFixedUpdate.Count; c++) {
            componentsFixedUpdate[c].FixedUpdate();
        }
    }
    internal static void Update () {
        for (int c = 0; c < componentsUpdate.Count; c++) {
            componentsUpdate[c].Update();
        }
    }
    internal static void UpdateRender () {
        for (int c = 0; c < componentsRender.Count; c++) {
            componentsRender[c].Update();
        }
    }


    public static void ComponentRegister (Component component) {
        Type type = component.GetType();

        if (components.TryGetValue(type, out List<Component>? list)) {
            list.Add(component);
        } else components.Add(type, new List<Component>());

        if (component is IComponentUpdate iComponentUpdate) {
            componentsUpdate.Add(iComponentUpdate);

            if (component is IRenderComponent iRenderComponent) {
                componentsRender.Add(iComponentUpdate);
            }
        }
        if (component is IComponentFixedUpdate iComponentFixedUpdate) {
            componentsFixedUpdate.Add(iComponentFixedUpdate);
        }

        component.OnAdd();
    }
    public static void ComponentUnregister (Component component) {
        Type type = component.GetType();

        if (components.TryGetValue(type, out List<Component>? list)) {
            list.Remove(component);
        }

        if (component is IComponentUpdate iComponentUpdate) {
            componentsUpdate.Remove(iComponentUpdate);

            if (component is IRenderComponent iRenderComponent) {
                componentsRender.Remove(iComponentUpdate);
            }
        }
        if (component is IComponentFixedUpdate iComponentFixedUpdate) {
            componentsFixedUpdate.Remove(iComponentFixedUpdate);
        }

        component.OnDestroy();
    }
    public static void ComponentRegister (List<Component> components) {
        for (int c = 0; c < components.Count; c++) {
            ComponentRegister(components[c]);
        }
    }
    public static void ComponentRemove (List<Component> components) {
        for (int c = 0; c < components.Count; c++) {
            ComponentUnregister(components[c]);
        }
    }


}
