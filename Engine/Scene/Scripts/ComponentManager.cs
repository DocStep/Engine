using Engine.Graphics;

namespace Engine;


public class ComponentManager : Singleton<ComponentManager> {

    public Action<Type>? de_RegisterType = null;

    private readonly Dictionary<Type, List<Component>> components = new Dictionary<Type, List<Component>>();
    public int componentsCount => components.Count;
    public Dictionary<Type, List<Component>> Components => components;

    private readonly List<IAwake> componentsAwake = new List<IAwake>();
    private readonly List<IAwake> _componentsAwake = new List<IAwake>();
    private readonly List<IStart> componentsStart = new List<IStart>();
    private readonly List<IStart> _componentsStart = new List<IStart>();
    private readonly List<IUpdate> componentsUpdate = new List<IUpdate>();
    private readonly List<IUpdate> componentsUpdateAlways = new List<IUpdate>();
    private readonly List<IFixedUpdate> componentsFixedUpdate = new List<IFixedUpdate>();
    private readonly List<IDrawRaw> componentsDrawRaw = new List<IDrawRaw>();

    public List<IUpdate> ComponentsUpdate => componentsUpdate;



    protected override void Init () {
        Type[] types = Reflection.FindAllSubclasses<Component>();
        for (int t = 0; t < types.Length; t++) {
            components.Add(types[t], new List<Component>());
            //Log.log("ComponentManager.ComponentRegister", types[t]);
            //de_RegisterType?.Invoke(types[t]);
        }

        //RegisterAll();

        Engine.Instance.de_AfterUpdate += GameObject.Flush;
        Renderer.Instance.de_DrawUI += DrawRaw;
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
    public void UpdateAlways () {
        for (int c = 0; c < componentsUpdateAlways.Count; c++) {
            componentsUpdateAlways[c].Update();
        }
    }
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

        if (component is IAwake iComponentAwake) {
            componentsAwake.Add(iComponentAwake);
        }
        if (component is IStart iComponentStart) {
            componentsStart.Add(iComponentStart);
        }
        if (component is IUpdate iComponentUpdate) {
            componentsUpdate.Add(iComponentUpdate);

            if (component is IUpdateAtFreeze iRenderComponent) {
                componentsUpdateAlways.Add(iComponentUpdate);
            }
        }
        if (component is IDrawRaw iComponentDrawRaw) {
            componentsDrawRaw.Add(iComponentDrawRaw);
        }
        if (component is IFixedUpdate iComponentFixedUpdate) {
            componentsFixedUpdate.Add(iComponentFixedUpdate);
        }

        //Log.log(component.Name, LogType.warning);
        component.OnAdd();
        //Log.log("ComponentManager.ComponentRegister", component);
    }
    internal void ComponentUnregister (Component component) {
        //Log.log(nameof(ComponentUnregister),  component);
        Type type = component.GetType();

        if (components.TryGetValue(type, out List<Component>? list)) {
            list.Remove(component);
        } else {
            string message = $"Error: Component Type ({component.GetType()}) was not registered";
            Log.log(message, LogType.error);
            throw new Exception(message);
        }

        if (component is IStart iComponentStart) {
            componentsStart.Remove(iComponentStart);
        }
        if (component is IUpdate iComponentUpdate) {
            componentsUpdate.Remove(iComponentUpdate);

            if (component is IUpdateAtFreeze iRenderComponent) {
                componentsUpdateAlways.Remove(iComponentUpdate);
            }
        }
        if (component is IFixedUpdate iComponentFixedUpdate) {
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
