using System.Linq;
using System.Reflection;
using Engine.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Engine;

public enum PrimitiveTypes {
    Cube,
    Sphere,
    Capsule,
    Plane,

    GizmoCube,
    GizmoSphere,
    GizmoCapsule,
    GizmoPlane,
}


public class GameObject : IDisposable, IAsset<GameObject> {
    public GameObject () {
        Id = lib.Id;
        InitTransform();
        SceneManager.ActiveScene.GameObjectAdd(this);
    }
    /// Used only by the deserializer — sets up JsonIgnore'd runtime state
    /// without touching the scene or generating a throwaway Id
    [JsonConstructor]
    private GameObject (bool _deserializing) {
        InitTransform();
        SceneManager.ActiveScene.GameObjectAdd(this);
    }
    void InitTransform () {
        Transform tr = new Transform();
        TransformHandle = new TransformHandle(tr);
        SetTransform(tr);
    }
    public GameObject (PrimitiveTypes primitive, Vector3 position = new Vector3(), 
        Vector3 rotation = new Vector3(), Vector3 scale = default, string? name = default) {
        if (string.IsNullOrEmpty(name)) name = primitive.GetType().Name;
        if (scale.Equals(default)) scale = Vector3.One;

        Id = lib.Id;
        Transform tr = new Transform();
        TransformHandle = new TransformHandle(tr);
        SetTransform(tr);
        Transform.Position = position;
        Transform.Rotation = Mathf.EulerToQuaternion(rotation);
        Transform.LocalScale = scale;
        
        MeshComponent mesh = AddComponent<MeshComponent>();
        switch (primitive) {
            case PrimitiveTypes.Cube:
                mesh.Mesh = AssetsEngine._mesh_Cube;
                //AddComponent<PhysicsComponent>();
                break;
            case PrimitiveTypes.Sphere:
                mesh.Mesh = AssetsEngine._mesh_Sphere;
                //AddComponent<PhysicsComponent>();
                break;
            case PrimitiveTypes.Capsule:
                mesh.Mesh = AssetsEngine._mesh_Capsule;
                //AddComponent<PhysicsComponent>();
                break;
            case PrimitiveTypes.Plane:
                mesh.Mesh = AssetsEngine._mesh_Plane;
                //AddComponent<PhysicsComponent>();
                break;

            /// <> <!>
            case PrimitiveTypes.GizmoCube:
                //MeshComponent colliderMesh = AddComponent<MeshComponent>();
                //colliderMesh.mesh = Gizmos._mesh_CubeWireframe;
                //colliderMesh.material = Gizmos._mat_GizmosGreen;
                break;
            case PrimitiveTypes.GizmoSphere:
                //colliderMesh = AddComponent<MeshComponent>();
                //colliderMesh.mesh = Gizmos._mesh_SphereWireframe;
                //colliderMesh.material = Gizmos._mat_GizmosGreen;
                break;
            case PrimitiveTypes.GizmoCapsule:
                //colliderMesh = AddComponent<MeshComponent>();
                //colliderMesh.mesh = Gizmos._mesh_CapsuleWireframe;
                //colliderMesh.material = Gizmos._mat_GizmosGreen;
                break;
            case PrimitiveTypes.GizmoPlane:
                //colliderMesh = AddComponent<MeshComponent>();
                //colliderMesh.mesh = Gizmos._mesh_PlaneWireframe;
                //colliderMesh.material = Gizmos._mat_GizmosGreen;
                break;
            default:
                break;
        }
        //mesh.material = AssetsEngine._mat_Lit;
        SceneManager.ActiveScene.GameObjectAdd(this);
    }

    [JsonIgnore] public readonly static string typeName = typeof(GameObject).Name;

    public string Name { get; set; } = TypeName;
    public long Id { get; set; }
    public string? Path { get; set; }

    public bool Enabled = true;
    [JsonIgnore, Hide] private TransformHandle TransformHandle = null!;
    public Transform Transform => TransformHandle.Current;

    public readonly List<Component> Components = new List<Component>();
    [JsonIgnore, Hide] private bool destroyed = false;

    [JsonIgnore] public const string TypeName = nameof(GameObject);


    public T? GetComponent<T> () where T : Component {
        if (Transform is T transform) return transform;

        foreach (Component component in Components) {
            if (component is T match) return match;
        }
        //throw new Exception($"Component of type {typeof(T)} not found in GameObject {Name}");
        return null;
    }
    public T AddComponent<T> () where T : Component, new() => AddComponentInternal(new T());
    public T AddComponentInternal<T> (T component) where T : Component {
        if (component is Transform transform) {
            SetTransform(transform);
            return component;
        }

        Components.Add(component);
        component.SetParent(this);
        ComponentsManager.Instance.ComponentRegister(component);
        return component;
    }

    public void RemoveComponent<T> () where T : Component, new() {
        T? component = null;
        foreach (Component comp in Components) {
            if (comp is T match) {
                component = match;
                break;
            }
        }
        if (component is not null) 
            ComponentsManager.Instance.ComponentUnregister(component);
    }
    public void RemoveComponent (Component component) {
        if (component is Transform) return;

        component.gameObject = null!;
        Components.Remove(component);
        ComponentsManager.Instance.ComponentUnregister(component);
    }


    internal void SetTransform (Transform transform) {
        Transform? previous = TransformHandle.Current;
        transform.SetParent(this);

        if (previous is not null)
            transform.CopyFrom(previous);

        TransformHandle.Rebind(transform);
    }


    [JsonIgnore, Hide] public bool Destroyed = false;
    [JsonIgnore, Hide] private static readonly System.Collections.Concurrent.ConcurrentQueue<GameObject> DestroyQueue =
        new System.Collections.Concurrent.ConcurrentQueue<GameObject>();

    /// <summary>Safe to call from any thread.</summary>
    private static void EnqueueDestroy (GameObject go) {
        DestroyQueue.Enqueue(go);
    }

    public void Destroy () {
        if (Destroyed) return;
        Destroyed = true;
        EnqueueDestroy(this);
    }
    public void DestroyImmediate () {
        Transform.Parent = null;
        destroyed = true;

        int count = Components.Count;
        for (int i = count - 1; i >= 0; i--) {
            ComponentsManager.Instance.ComponentUnregister(Components[i]);
        }
        Components.Clear();

        ComponentsManager.Instance.ComponentUnregister(Transform);
        SceneManager.ActiveScene.GameObjects.Remove(this);

        Dispose();
    }

    /// <summary>Main thread only. Call once per frame before Update().</summary>
    public static void Flush () {
        //Log.log();
        while (DestroyQueue.TryDequeue(out GameObject? go)) {
            go.DestroyImmediate();
        }
    }



    private static readonly BindingFlags FieldFlags = BindingFlags.Public | BindingFlags.Instance;

    /// <summary>Non-generic counterpart to AddComponentInternal&lt;T&gt;, for when the component
    /// type is only known at runtime (loading a prefab by its serialized type name).</summary>
    public Component AddComponentInternal (Component component) {
        if (component is Transform transform) {
            SetTransform(transform);
            return component;
        }

        Components.Add(component);
        component.SetParent(this);
        ComponentsManager.Instance.ComponentRegister(component);
        return component;
    }

    /// <summary>Saves this GameObject and its full child hierarchy as a flat prefab — every
    /// GameObject, Transform and Component is its own top-level entry linked by $id/$ref,
    /// same shape as Unity's fileID-linked documents (not nested inside each other).</summary>
    public void Save (string path) {
        Path = path;
        PrefabContext ctx = new PrefabContext();
        List<GameObject> flat = new List<GameObject>();
        Flatten(this, flat);

        int nextId = 0;
        foreach (GameObject go in flat) {
            ctx.WriteIds[go] = nextId++;
            ctx.WriteIds[go.Transform] = nextId++;
            foreach (Component c in go.Components) {
                ctx.WriteIds[c] = nextId++;

                foreach (FieldInfo field in c.GetType().GetFields(FieldFlags)) {
                    if (field.IsDefined(typeof(JsonIgnoreAttribute))) continue;
                    object? value = field.GetValue(c);
                    if (value is IAsset asset && asset.Path == null && !ctx.WriteIds.ContainsKey(asset))
                        ctx.WriteIds[asset] = nextId++;
                }
            }
        }

        JArray objects = new JArray();
        foreach (GameObject go in flat) {
            objects.Add(WriteGameObjectEntry(go, ctx));
            objects.Add(WriteTransformEntry(go.Transform, ctx));
            foreach (Component c in go.Components) objects.Add(WriteComponentEntry(c, ctx));
        }

        JObject json = new JObject { ["Root"] = ctx.WriteIds[this], ["Objects"] = objects };
        File.WriteAllText(path, json.ToString(Formatting.Indented));
    }

    /// <summary>Loads a prefab previously written by Save().</summary>
    public static GameObject Load (string path) {
        JObject json = JObject.Parse(File.ReadAllText(path));
        JArray objects = (JArray)json["Objects"]!;
        Dictionary<int, JObject> byId = objects.Cast<JObject>().ToDictionary(o => o["$id"]!.Value<int>());
        PrefabContext ctx = new PrefabContext();

        // Pass 1: instantiate every GameObject and Component up front (fields still empty/default),
        // so every $ref in pass 2 resolves regardless of what order entries appear in the file.
        foreach (JObject entry in objects.Cast<JObject>()) {
            if (entry["Type"]!.Value<string>() != "GameObject") continue;

            GameObject go = new GameObject() { Name = entry["Name"]!.Value<string>()! };
            ctx.ReadObjects[entry["$id"]!.Value<int>()] = go;
            ctx.ReadObjects[entry["Transform"]!["$ref"]!.Value<int>()] = go.Transform;

            foreach (JToken componentRef in entry["Components"]!) {
                int componentId = componentRef.Value<int>();
                JObject componentEntry = byId[componentId];
                Type type = ComponentTypes.Resolve(componentEntry["Type"]!.Value<string>()!);
                Component component = (Component)Activator.CreateInstance(type)!;
                go.AddComponentInternal(component);
                ctx.ReadObjects[componentId] = component;
            }
        }

        // Pass 2: fill in every Transform and Component entry — reference-typed fields resolve
        // through ctx now that every object in the prefab exists.
        foreach (JObject entry in objects.Cast<JObject>()) {
            string type = entry["Type"]!.Value<string>()!;
            if (type == "GameObject") continue;

            if (type == "Transform") {
                Transform tr = (Transform)ctx.ReadObjects[entry["$id"]!.Value<int>()];
                tr.LocalPosition = entry["LocalPosition"]!.ToObject<Vector3>();
                tr.LocalEuler = entry["LocalEuler"]!.ToObject<Vector3>();
                tr.LocalScale = entry["LocalScale"]!.ToObject<Vector3>();
                JToken parentRef = entry["Parent"]!;
                if (parentRef.Type != JTokenType.Null) {
                    tr.Parent = (Transform)ctx.ReadObjects[parentRef["$ref"]!.Value<int>()];
                }
                continue;
            }

            Component c = (Component)ctx.ReadObjects[entry["$id"]!.Value<int>()];
            ReadFields(c, entry, ctx);
        }

        return (GameObject)ctx.ReadObjects[json["Root"]!.Value<int>()];
    }

    private static void Flatten (GameObject go, List<GameObject> into) {
        into.Add(go);
        foreach (Transform child in go.Transform.Children) {
            Flatten(child.gameObject, into);
        }
    }

    private static JObject WriteGameObjectEntry (GameObject go, PrefabContext ctx) {
        return new JObject {
            ["$id"] = ctx.WriteIds[go],
            ["Type"] = "GameObject",
            ["Name"] = go.Name,
            ["Transform"] = new JObject { ["$ref"] = ctx.WriteIds[go.Transform] },
            ["Components"] = new JArray(go.Components.Select(c => ctx.WriteIds[c])),
        };
    }
    private static JObject WriteTransformEntry (Transform tr, PrefabContext ctx) {
        return new JObject {
            ["$id"] = ctx.WriteIds[tr],
            ["Type"] = "Transform",
            ["GameObject"] = new JObject { ["$ref"] = ctx.WriteIds[tr.gameObject] },
            ["LocalPosition"] = JToken.FromObject(tr.LocalPosition),
            ["LocalEuler"] = JToken.FromObject(tr.LocalEuler),
            ["LocalScale"] = JToken.FromObject(tr.LocalScale),
            ["Parent"] = tr.Parent != null && ctx.WriteIds.TryGetValue(tr.Parent, out int pid)
                ? new JObject { ["$ref"] = pid }
                : JValue.CreateNull(),
        };
    }

    /// <summary>Reflects over a component's public fields and writes each one — no ToJObj
    /// needed on the component itself. Reference-typed fields become {"$ref": id}.</summary>
    private static JObject WriteComponentEntry (Component c, PrefabContext ctx) {
        JObject obj = new JObject {
            ["$id"] = ctx.WriteIds[c],
            ["Type"] = c.GetType().Name,
            ["GameObject"] = new JObject { ["$ref"] = ctx.WriteIds[c.gameObject] },
        };
        foreach (FieldInfo field in c.GetType().GetFields(FieldFlags)) {
            if (field.IsDefined(typeof(JsonIgnoreAttribute))) continue;
            obj[field.Name] = WriteValue(field.GetValue(c), ctx);
        }
        return obj;
    }

    private static JToken WriteValue (object? value, PrefabContext ctx) {
        if (value is null) return JValue.CreateNull();
        if (value is Transform transform) {
            return ctx.WriteIds.TryGetValue(transform, out int id) ? new JObject { ["$ref"] = id } : JValue.CreateNull();
        }
        if (value is GameObject refGo) {
            return ctx.WriteIds.TryGetValue(refGo, out int id) ? new JObject { ["$ref"] = id } : JValue.CreateNull();
        }
        if (value is IAsset asset) {
            if (asset.Path != null) return JToken.FromObject(asset.Path);
            return ctx.WriteIds.TryGetValue(asset, out int id) ? new JObject { ["$ref"] = id } : JValue.CreateNull();
        }
        return JToken.FromObject(value);
    }

    /// <summary>Mirror of WriteComponentEntry for load — sets each public field back from JSON,
    /// resolving {"$ref": id} through ctx instead of deserializing it as data.</summary>
    private static void ReadFields (Component c, JObject obj, PrefabContext ctx) {
        foreach (FieldInfo field in c.GetType().GetFields(FieldFlags)) {
            if (field.IsDefined(typeof(JsonIgnoreAttribute))) continue;
            if (!obj.TryGetValue(field.Name, out JToken? token) || token == null) continue;
            field.SetValue(c, ReadValue(field.FieldType, token, ctx));
        }
    }

    [JsonIgnore] private static readonly MethodInfo AssetsLoadMethod = typeof(Assets).GetMethod(nameof(Assets.Load))!;

    private static object? ReadValue (Type fieldType, JToken token, PrefabContext ctx) {
        if (token.Type == JTokenType.Null) return null;
        if (typeof(Transform).IsAssignableFrom(fieldType)) {
            return ctx.ReadObjects.TryGetValue(token["$ref"]!.Value<int>(), out object? o) ? (Transform)o : null;
        }
        if (typeof(GameObject).IsAssignableFrom(fieldType)) {
            return ctx.ReadObjects.TryGetValue(token["$ref"]!.Value<int>(), out object? o) ? (GameObject)o : null;
        }
        if (typeof(IAsset).IsAssignableFrom(fieldType)) {
            return AssetsLoadMethod.MakeGenericMethod(fieldType).Invoke(null, new object[] { token.Value<string>()! });
        }
        return token.ToObject(fieldType);
    }



    public void Dispose () {

    }

}
