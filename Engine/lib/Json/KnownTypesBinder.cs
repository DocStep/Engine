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
        if (!typeToName.TryGetValue(serializedType, out typeName)) {
            typeName = serializedType.FullName;
            typeToName[serializedType] = typeName;
        }

        assemblyName = null; // optional: leave this null for shorter JSON
    }

    public Type BindToType (string assemblyName, string typeName) {
        if (!nameToType.TryGetValue(typeName, out Type? type)) {
            foreach (Type known in KnownTypes) {
                if (known.FullName == typeName) {
                    type = known;
                    nameToType[typeName] = type;
                    break;
                }
            }
        }

        return type;
    }

}
