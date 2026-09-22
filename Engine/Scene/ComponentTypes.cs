using System.Reflection;
using System.Linq;

namespace Engine;


public static class ComponentTypes {
    private static Dictionary<string, Type>? _types;

    public static Type Resolve (string name) {
        if (_types == null) {
            _types = new Dictionary<string, Type>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                Type[] types;
                try {
                    types = assembly.GetTypes();
                } catch (ReflectionTypeLoadException e) {
                    types = e.Types.Where(t => t != null).ToArray()!;
                }
                foreach (Type type in types) {
                    if (typeof(Component).IsAssignableFrom(type) && !type.IsAbstract) {
                        _types[type.Name] = type;
                    }
                }
            }
        }

        return _types[name];
    }
}
