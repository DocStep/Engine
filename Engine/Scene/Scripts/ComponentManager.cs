namespace Engine;


public class ComponentManager : Singleton<ComponentManager> {

    private readonly Dictionary<Type, List<Component>> components =  new Dictionary<Type, List<Component>>();
    private readonly List<IComponentUpdate> componentsUpdate = new List<IComponentUpdate>();
    private readonly List<IComponentFixedUpdate> componentsFixedUpdate = new List<IComponentFixedUpdate>();
    public int componentsCount => components.Count;

    private readonly List<IComponentUpdate> componentsRender = new List<IComponentUpdate>();


    protected override void Init () {
        Type[] types = Reflection.FindAllSubclasses<Component>();
        for (int t = 0; t < types.Length; t++) {
            components.Add(types[t], new());
        }
    }

    internal void FixedUpdate () {
        for (int c = 0; c < componentsFixedUpdate.Count; c++) {
            componentsFixedUpdate[c].FixedUpdate();
        }
    }
    internal void Update () {
        for (int c = 0; c < componentsUpdate.Count; c++) {
            componentsUpdate[c].Update();
        }
    }
    internal void UpdateRender () {
        for (int c = 0; c < componentsRender.Count; c++) {
            componentsRender[c].Update();
        }
    }


    public void ComponentRegister (Component component) {
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
    public void ComponentUnregister (Component component) {
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
