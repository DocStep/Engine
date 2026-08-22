using Engine.Graphics;

namespace Engine;


public class ComponentManager : Singleton<ComponentManager> {

    public Action<Type>? de_RegisterType = null;

    private readonly Dictionary<Type, List<Component>> components = new Dictionary<Type, List<Component>>();
    public int componentsCount => components.Count;
    public Dictionary<Type, List<Component>> Components => components;

    private readonly List<IComponentAwake> componentsAwake = new List<IComponentAwake>();
    private readonly List<IComponentAwake> _componentsAwake = new List<IComponentAwake>();
    private readonly List<IComponentStart> componentsStart = new List<IComponentStart>();
    private readonly List<IComponentStart> _componentsStart = new List<IComponentStart>();
    private readonly List<IComponentUpdate> componentsUpdate = new List<IComponentUpdate>();
    private readonly List<IComponentUpdate> componentsUpdateAtFreeze = new List<IComponentUpdate>();
    private readonly List<IComponentFixedUpdate> componentsFixedUpdate = new List<IComponentFixedUpdate>();
    private readonly List<IComponentDrawRaw> componentsDrawRaw = new List<IComponentDrawRaw>();

    public List<IComponentUpdate> ComponentsUpdate => componentsUpdate;



    protected override void Init () {
        Type[] types = Reflection.FindAllSubclasses<Component>();
        for (int t = 0; t < types.Length; t++) {
            components.Add(types[t], new List<Component>());
            //Log.log("ComponentManager.ComponentRegister", types[t]);
            //de_RegisterType?.Invoke(types[t]);
        }

        //RegisterAll();
    }

    public void Update () {
        int count;

        _componentsAwake.AddRange(componentsAwake);
        componentsAwake.Clear();
        count = _componentsAwake.Count;
        for (int c = 0; c < count; c++) {
            if (!_componentsAwake[c].Enabled) continue;
            _componentsAwake[c].Awake();
        }
        _componentsAwake.Clear();

        _componentsStart.AddRange(componentsStart);
        componentsStart.Clear();
        count = _componentsStart.Count;
        for (int c = 0; c < count; c++) {
            if (!_componentsStart[c].Enabled) continue;
            _componentsStart[c].Start();
        }
        _componentsStart.Clear();

        count = componentsUpdate.Count;
        for (int c = 0; c < count; c++) {
            if (!componentsUpdate[c].Enabled) continue;
            componentsUpdate[c].Update();
        }
    }
    public void DrawRaw () {
        int count;

        count = componentsDrawRaw.Count;
        for (int c = 0; c < count; c++) {
            if (componentsDrawRaw[c].Enabled) 
                componentsDrawRaw[c].DrawRaw();
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


    internal void ComponentRegister (Component component) {
        Type type = component.GetType();
        if (components.TryGetValue(type, out List<Component>? list)) {
            components[type].Add(component);
        } else {
            string message = $"Error: Component Type ({component.GetType()}) was not registered";
            Log.log(message, LogType.error);
            throw new Exception(message);
        }

        if (component is IComponentAwake iComponentAwake) {
            componentsAwake.Add(iComponentAwake);
        }
        if (component is IComponentStart iComponentStart) {
            componentsStart.Add(iComponentStart);
        }
        if (component is IComponentUpdate iComponentUpdate) {
            componentsUpdate.Add(iComponentUpdate);

            if (component is IUpdateAtFreeze iRenderComponent) {
                componentsUpdateAtFreeze.Add(iComponentUpdate);
            }
        }
        if (component is IComponentDrawRaw iComponentDrawRaw) {
            componentsDrawRaw.Add(iComponentDrawRaw);
        }
        if (component is IComponentFixedUpdate iComponentFixedUpdate) {
            componentsFixedUpdate.Add(iComponentFixedUpdate);
        }

        //Log.log(component.Name, LogType.warning);
        component.OnAdd();
        //Log.log("ComponentManager.ComponentRegister", component);
    }
    internal void ComponentUnregister (Component component) {
        Type type = component.GetType();

        if (components.TryGetValue(type, out List<Component>? list)) {
            list.Remove(component);
        } else {
            string message = $"Error: Component Type ({component.GetType()}) was not registered";
            Log.log(message, LogType.error);
            throw new Exception(message);
        }

        if (component is IComponentStart iComponentStart) {
            componentsStart.Remove(iComponentStart);
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
        //Log.log("ComponentManager.ComponentUnregister", component);
    }
    /*public void ComponentRegister (List<Component> components) {
        for (int c = 0; c < components.Count; c++) {
            ComponentRegister(components[c]);
        }
    }
    public void ComponentRemove (List<Component> components) {
        for (int c = 0; c < components.Count; c++) {
            ComponentUnregister(components[c]);
        }
    }*/


    private static readonly Dictionary<Type, Func<object>> factories = new Dictionary<Type, Func<object>>();

    public static void RegisterAll () {
        Type componentType = typeof(Component);

        foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            foreach (Type type in assembly.GetTypes()) {
                if (!componentType.IsAssignableFrom(type)) continue;
                if (type.IsAbstract || type.IsInterface) continue;

                System.Reflection.ConstructorInfo ctor = type.GetConstructor(Type.EmptyTypes);
                if (ctor == null) continue;

                factories[type] = () => Activator.CreateInstance(type);
            }
        }
    }

    public static object Create (Type type) {
        return factories[type]();
    }

}
