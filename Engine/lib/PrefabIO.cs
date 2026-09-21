using System.Runtime.CompilerServices;
using System.Reflection;
using System.Linq;
using Newtonsoft.Json.Linq; /// JObject, JArray, JToken

namespace Engine;

public static class PrefabIO {

    public static void Save (GameObject root, string path) {
        var file = new PrefabFile();
        Collect(root, file);

        Json.Write(path, file);
    }

    static void Collect (GameObject go, PrefabFile file) {
        file.Blocks.Add(new Block {
            Id = go.Id,
            Type = "GameObject",
            Data = new JObject {
                ["Name"] = go.Name,
                ["Enabled"] = go.Enabled,
                ["Transform"] = go.Transform.Id,
                ["Components"] = new JArray(go.Components.Select(c => c.Id))
            }
        });

        file.Blocks.Add(new Block {
            Id = go.Transform.Id,
            Type = "Transform",
            Data = new JObject {
                ["GameObject"] = go.Id,
                ["Parent"] = go.Transform.Parent?.Id ?? 0,
                ["Children"] = new JArray(go.Transform.Children.Select(t => t.Id)),
                ["Position"] = JToken.FromObject(go.Transform.Position),
                ["Rotation"] = JToken.FromObject(go.Transform.Rotation),
                ["LocalScale"] = JToken.FromObject(go.Transform.LocalScale)
            }
        });

        foreach (Component c in go.Components)
            file.Blocks.Add(WriteComponent(c, go.Id));

        foreach (Transform child in go.Transform.Children)
            Collect(child.gameObject, file);
    }

    static Block WriteComponent (Component c, long ownerGoId) {
        JObject data = new JObject { ["GameObject"] = ownerGoId };

        foreach (FieldInfo f in c.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)) {
            if (f.GetCustomAttribute<RefAttribute>() != null) {
                var target = f.GetValue(c) as Component;
                data[f.Name] = target?.Id ?? 0; /// ref field -> id, not inline data
            } else {
                data[f.Name] = JToken.FromObject(f.GetValue(c)!);
            }
        }

        return new Block { Id = c.Id, Type = c.GetType().Name, Data = data };
    }


    public static GameObject? Load (string path) {
        PrefabFile file = Json.Read<PrefabFile>(path)!;

        var goMap = new Dictionary<long, GameObject>();
        var trMap = new Dictionary<long, Transform>();
        var compMap = new Dictionary<long, Component>();

        /// pass 1 — create every object, no refs resolved yet
        foreach (Block b in file.Blocks) {
            switch (b.Type) {
                case "GameObject":
                    goMap[b.Id] = new GameObject();
                    break;
                case "Transform":
                    trMap[b.Id] = new Transform();
                    break;
                default:
                    compMap[b.Id] = CreateComponent(b.Type);
                    break;
            }
        }

        /// pass 2 — fill in fields + resolve every id reference
        foreach (Block b in file.Blocks) {
            switch (b.Type) {
                case "GameObject":
                    ResolveGameObject(goMap[b.Id], b.Data, goMap, trMap, compMap);
                    break;
                case "Transform":
                    ResolveTransform(trMap[b.Id], b.Data, goMap, trMap);
                    break;
                default:
                    ResolveComponent(compMap[b.Id], b.Data, goMap, compMap);
                    break;
            }
        }

        long rootId = FindRootGoId(file, trMap);
        return goMap.TryGetValue(rootId, out GameObject? root) ? root : null;
    }

    static Component CreateComponent (string type) => type switch {
        "ChunksGrid" => new ChunksGrid(),
        "MeshComponent" => new Graphics.MeshComponent(),
        "BoxColliderComponent" => new BoxColliderComponent(),
        _ => throw new InvalidOperationException($"Unknown component type '{type}'.")
    };

    static void ResolveGameObject (GameObject go, JObject data,
        Dictionary<long, GameObject> goMap, Dictionary<long, Transform> trMap, Dictionary<long, Component> compMap) {

        go.Name = data["Name"]!.Value<string>()!;
        go.Enabled = data["Enabled"]!.Value<bool>();

        foreach (JToken compId in data["Components"]!) {
            Component c = compMap[compId.Value<long>()];
            c.gameObject = go;
            go.Components.Add(c);
            ComponentsManager.Instance.ComponentRegister(c);
        }
    }

    static void ResolveTransform (Transform tr, JObject data,
        Dictionary<long, GameObject> goMap, Dictionary<long, Transform> trMap) {

        GameObject owner = goMap[data["GameObject"]!.Value<long>()];
        tr.gameObject = owner;
        owner.SetTransform(tr); /// wires TransformHandle, same as constructors already do

        tr.Position = data["Position"]!.ToObject<Vector3>();
        tr.Rotation = data["Rotation"]!.ToObject<Quaternion>();
        tr.LocalScale = data["LocalScale"]!.ToObject<Vector3>();

        foreach (JToken childId in data["Children"]!)
            trMap[childId.Value<long>()].SetParent(tr); /// wires Parent + Children both ways
    }

    static void ResolveComponent (Component c, JObject data,
        Dictionary<long, GameObject> goMap, Dictionary<long, Component> compMap) {

        foreach (FieldInfo f in c.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)) {
            if (!data.ContainsKey(f.Name)) continue;

            if (f.GetCustomAttribute<RefAttribute>() != null) {
                long refId = data[f.Name]!.Value<long>();
                if (refId != 0)
                    f.SetValue(c, compMap[refId]);
            } else {
                f.SetValue(c, data[f.Name]!.ToObject(f.FieldType));
            }
        }
    }

    static long FindRootGoId (PrefabFile file, Dictionary<long, Transform> trMap) {
        /// root = the GameObject whose Transform has Parent == 0
        Block trRoot = file.Blocks.First(b => b.Type == "Transform" && b.Data["Parent"]!.Value<long>() == 0);
        return trRoot.Data["GameObject"]!.Value<long>();
    }

}
