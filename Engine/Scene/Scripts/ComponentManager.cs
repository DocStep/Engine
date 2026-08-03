using Engine.Graphics;

namespace Engine;


public class ComponentManager : Singleton<ComponentManager> {

    private readonly Dictionary<Type, List<Component>> components = new Dictionary<Type, List<Component>>();
    public int componentsCount => components.Count;

    private readonly List<IComponentUpdate> componentsUpdate = new List<IComponentUpdate>();
    private readonly List<IComponentUpdate> componentsUpdateAtFreeze = new List<IComponentUpdate>();
    private readonly List<IComponentFixedUpdate> componentsFixedUpdate = new List<IComponentFixedUpdate>();

    public List<IComponentUpdate> ComponentsUpdate => componentsUpdate;


    protected override void Init () {
        Type[] types = Reflection.FindAllSubclasses<Component>();
        for (int t = 0; t < types.Length; t++) {
            components.Add(types[t], new List<Component>());
        }
    }

    public void Update () {
        for (int c = 0; c < componentsUpdate.Count; c++) {
            if (!componentsUpdate[c].Enabled) continue;
            componentsUpdate[c].Update();
        }
    }
    /*public void UpdateAtFreeze () {
        Log.log("UpdateAtFreeze", componentsUpdateAtFreeze.Count);
        for (int c = 0; c < componentsUpdateAtFreeze.Count; c++) {
            componentsUpdateAtFreeze[c].Update();
        }
    }*/
    public void FixedUpdate () {
        for (int c = 0; c < componentsFixedUpdate.Count; c++) {
            if (!componentsFixedUpdate[c].Enabled) continue;
            componentsFixedUpdate[c].FixedUpdate();
        }
    }


    public void ComponentRegister (Component component) {
        Type type = component.GetType();

        if (components.TryGetValue(type, out List<Component>? list)) {
            list.Add(component);
        } else components.Add(type, new List<Component>());

        if (component is IComponentUpdate iComponentUpdate) {
            componentsUpdate.Add(iComponentUpdate);

            if (component is IUpdateAtFreeze iRenderComponent) {
                componentsUpdateAtFreeze.Add(iComponentUpdate);
            }
        }
        if (component is IComponentFixedUpdate iComponentFixedUpdate) {
            componentsFixedUpdate.Add(iComponentFixedUpdate);
        }

        component.OnAdd();
    }
    public void ComponentUnregister (Component component) {
        Type type = component.GetType();

        if (components.TryGetValue(type, out List<Component>? list)) {
            list.Remove(component);
        }

        if (component is IComponentUpdate iComponentUpdate) {
            componentsUpdate.Remove(iComponentUpdate);

            if (component is IUpdateAtFreeze iRenderComponent) {
                componentsUpdateAtFreeze.Remove(iComponentUpdate);
            }
        }
        if (component is IComponentFixedUpdate iComponentFixedUpdate) {
            componentsFixedUpdate.Remove(iComponentFixedUpdate);
        }

        component.OnRemove();
    }
    public void ComponentRegister (List<Component> components) {
        for (int c = 0; c < components.Count; c++) {
            ComponentRegister(components[c]);
        }
    }
    public void ComponentRemove (List<Component> components) {
        for (int c = 0; c < components.Count; c++) {
            ComponentUnregister(components[c]);
        }
    }


}
