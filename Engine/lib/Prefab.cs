using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Engine;


public static class Prefab {

    private static readonly BindingFlags FieldFlags = BindingFlags.Public | BindingFlags.Instance;
    private static readonly MethodInfo AssetsLoadMethod = typeof(Assets).GetMethod(nameof(Assets.Load))!;


    public static void Save (GameObject go, string path) {
        go.Path = path;
        SaveObjects(new List<GameObject> { go }, path);
    }

    public static GameObject Load (string path) {
        return LoadObjects(path)[0];
    }



    private sealed class ShortNameBinder : Newtonsoft.Json.Serialization.ISerializationBinder {
        private static readonly Dictionary<string, Type> ByName = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .GroupBy(t => t.Name)
            .ToDictionary(g => g.Key, g => g.First()); // assumes unique short names across loaded assemblies

        public Type BindToType (string? assemblyName, string typeName) => ByName[typeName];

        public void BindToName (Type serializedType, out string? assemblyName, out string? typeName) {
            assemblyName = null;
            typeName = serializedType.Name;
        }
    }

    private static readonly JsonSerializer PolymorphicSerializer = new JsonSerializer {
        TypeNameHandling = TypeNameHandling.Auto,
        SerializationBinder = new ShortNameBinder(),
    };



    /// Serializes any set of GameObject trees (roots + all descendants) to one file.
    /// Used directly by Scene; Prefab.Save wraps it with a single root.
    public static void SaveObjects (List<GameObject> roots, string path) {
        PrefabContext ctx = new PrefabContext();
        List<GameObject> flat = new List<GameObject>();
        HashSet<GameObject> seen = new HashSet<GameObject>();
        foreach (GameObject root in roots) Flatten(root, flat, seen);
        AssignWriteIds(flat, ctx);

        JObject json = new JObject {
            ["Roots"] = new JArray(roots.Where(seen.Contains).Select(r => ctx.WriteIds[r])),
            ["Objects"] = WriteObjects(flat, ctx),
        };
        File.WriteAllText(path, json.ToString(Formatting.Indented));
    }

    /// Mirror of SaveObjects — returns every root GameObject, in file order.
    public static List<GameObject> LoadObjects (string path) {
        JObject json = JObject.Parse(File.ReadAllText(path));
        PrefabContext ctx = new PrefabContext();
        ReadObjects((JArray)json["Objects"]!, ctx);
        return json["Roots"]!.Select(r => (GameObject)ctx.ReadObjects[r.Value<int>()]).ToList();
    }


    private static void Flatten (GameObject go, List<GameObject> into, HashSet<GameObject> seen) {
        if (!seen.Add(go)) return; // already flattened as someone else's child — skip
        into.Add(go);
        foreach (Transform child in go.Transform.Children) Flatten(child.gameObject, into, seen);
    }

    private static void AssignWriteIds (List<GameObject> flat, PrefabContext ctx) {
        int nextId = 0;
        foreach (GameObject g in flat) {
            ctx.WriteIds[g] = nextId++;
            ctx.WriteIds[g.Transform] = nextId++;
            foreach (Component c in g.Components) {
                ctx.WriteIds[c] = nextId++;

                foreach (FieldInfo field in c.GetType().GetFields(FieldFlags)) {
                    if (field.IsDefined(typeof(JsonIgnoreAttribute))) continue;
                    object? value = field.GetValue(c);
                    if (value is IAsset asset && asset.Path == null && !ctx.WriteIds.ContainsKey(asset))
                        ctx.WriteIds[asset] = nextId++;
                }
            }
        }
    }

    private static JArray WriteObjects (List<GameObject> flat, PrefabContext ctx) {
        JArray objects = new JArray();
        foreach (GameObject g in flat) {
            objects.Add(WriteGameObjectEntry(g, ctx));
            objects.Add(WriteTransformEntry(g.Transform, ctx));
            foreach (Component c in g.Components) objects.Add(WriteComponentEntry(c, ctx));
        }
        return objects;
    }

    /// Two-pass load: instantiate every GameObject/Component up front (fields still default)
    /// so every $ref in pass 2 resolves regardless of file order, then fill in fields.
    private static void ReadObjects (JArray objects, PrefabContext ctx) {
        Dictionary<int, JObject> byId = objects.Cast<JObject>().ToDictionary(o => o["$id"]!.Value<int>());

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

        foreach (JObject entry in objects.Cast<JObject>()) {
            string type = entry["Type"]!.Value<string>()!;
            if (type == "GameObject") continue;

            if (type == "Transform") {
                Transform tr = (Transform)ctx.ReadObjects[entry["$id"]!.Value<int>()];
                tr.LocalPosition = entry["LocalPosition"]!.ToObject<Vector3>();
                tr.LocalEuler = entry["LocalEuler"]!.ToObject<Vector3>();
                tr.LocalScale = entry["LocalScale"]!.ToObject<Vector3>();
                JToken parentRef = entry["Parent"]!;
                if (parentRef.Type != JTokenType.Null) tr.Parent = (Transform)ctx.ReadObjects[parentRef["$ref"]!.Value<int>()];
                continue;
            }

            Component c = (Component)ctx.ReadObjects[entry["$id"]!.Value<int>()];
            ReadFields(c, entry, ctx);
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
        if (value is Transform transform) return ctx.WriteIds.TryGetValue(transform, out int id) ? new JObject { ["$ref"] = id } : JValue.CreateNull();
        if (value is GameObject refGo) return ctx.WriteIds.TryGetValue(refGo, out int id) ? new JObject { ["$ref"] = id } : JValue.CreateNull();
        if (value is IAsset asset) {
            if (asset.Path != null) return JToken.FromObject(asset.Path);
            return ctx.WriteIds.TryGetValue(asset, out int id) ? new JObject { ["$ref"] = id } : JValue.CreateNull();
        }
        return JToken.FromObject(value, PolymorphicSerializer);
    }

    private static void ReadFields (Component c, JObject obj, PrefabContext ctx) {
        foreach (FieldInfo field in c.GetType().GetFields(FieldFlags)) {
            if (field.IsDefined(typeof(JsonIgnoreAttribute))) continue;
            if (!obj.TryGetValue(field.Name, out JToken? token) || token == null) continue;
            field.SetValue(c, ReadValue(field.FieldType, token, ctx));
        }
    }

    private static object? ReadValue (Type fieldType, JToken token, PrefabContext ctx) {
        if (token.Type == JTokenType.Null) return null;
        if (typeof(Transform).IsAssignableFrom(fieldType)) return ctx.ReadObjects.TryGetValue(token["$ref"]!.Value<int>(), out object? o) ? (Transform)o : null;
        if (typeof(GameObject).IsAssignableFrom(fieldType)) return ctx.ReadObjects.TryGetValue(token["$ref"]!.Value<int>(), out object? o) ? (GameObject)o : null;
        if (typeof(IAsset).IsAssignableFrom(fieldType)) {
            if (token.Type == JTokenType.Object)
                return ctx.ReadObjects.TryGetValue(token["$ref"]!.Value<int>(), out object? o) ? o : null;
            return AssetsLoadMethod.MakeGenericMethod(fieldType).Invoke(null, new object[] { token.Value<string>()! });
        }
        return token.ToObject(fieldType, PolymorphicSerializer);
    }

}
