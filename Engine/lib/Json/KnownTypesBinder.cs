using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace Engine;


public class KnownTypesBinder : ISerializationBinder {
    public KnownTypesBinder () {
        KnownTypes = new List<Type>();
    }

    public IList<Type> KnownTypes { get; set; }

    private Dictionary<string, Type> nameToType = new Dictionary<string, Type>();
    private Dictionary<Type, string> typeToName = new Dictionary<Type, string>();


    public void BindToName (Type serializedType, out string assemblyName, out string typeName) {
        if (!typeToName.TryGetValue(serializedType, out typeName)) {
            typeName = serializedType.FullName;
            typeToName[serializedType] = typeName;
        }

        assemblyName = null; // optional: leave this null for shorter JSON
    }

    public Type BindToType (string assemblyName, string typeName) {
        if (!nameToType.TryGetValue(typeName, out var type)) {
            foreach (var known in KnownTypes) {
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
