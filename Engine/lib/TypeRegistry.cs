using System.Reflection;

namespace Engine;


static class TypeRegistry {
    static readonly Dictionary<string, Type> byName = new Dictionary<string, Type>();

    static TypeRegistry () {
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
            foreach (Type t in asm.GetTypes()) {
                if (typeof(Component).IsAssignableFrom(t) && !t.IsAbstract)
                    byName[t.Name] = t;
            }
        }
    }

    public static Component Create (string typeName) {
        if (!byName.TryGetValue(typeName, out Type? t))
            throw new InvalidOperationException($"Unknown component type '{typeName}'.");
        return (Component)Activator.CreateInstance(t)!;
    }
}
