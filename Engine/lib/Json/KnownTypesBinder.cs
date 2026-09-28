using Newtonsoft.Json.Serialization;

namespace Engine;


public class KnownTypesBinder : ISerializationBinder {
    public KnownTypesBinder () {
        KnownTypes = new List<Type>();
    }

    public IList<Type> KnownTypes { get; set; }

    private Dictionary<string, Type> nameToType = new Dictionary<string, Type>();
    private Dictionary<Type, string> typeToName = new Dictionary<Type, string>();


    public void BindToName (Type serializedType, out string? assemblyName, out string? typeName) {
        if (!typeToName.TryGetValue(serializedType, out string? name)) {
            name = serializedType.FullName ?? serializedType.Name;
            typeToName[serializedType] = name;
        }
        typeName = name;

        assemblyName = null; // optional: leave this null for shorter JSON
    }

    public Type BindToType (string? assemblyName, string typeName) {
        if (!nameToType.TryGetValue(typeName, out Type? type)) {
            foreach (Type known in KnownTypes) {
                if (known.FullName == typeName) {
                    type = known;
                    nameToType[typeName] = type;
                    break;
                }
            }
        }

        return type ?? throw new TypeLoadException($"[Json] Unknown type: {typeName}");
    }

}
