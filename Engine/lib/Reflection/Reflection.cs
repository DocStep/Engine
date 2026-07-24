using System.Reflection;
using System.Linq;

namespace Engine;


public class Reflection : Singleton<Reflection> {

    public static Type[] FindAllSubclasses<T> (bool doAbstract = false) {
        Type baseType = typeof(T);

        List<Type> result = new List<Type>();
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            Type[] types;
            try {
                types = assembly.GetTypes();
            } catch (ReflectionTypeLoadException ex) {
                types = ex.Types.Where(type => type is not null).ToArray()!;
            }

            foreach (Type type in types) {
                if (type == baseType) continue;
                if (!baseType.IsAssignableFrom(type)) continue;
                if (type.IsInterface) continue;
                if (!doAbstract && type.IsAbstract) continue;

                result.Add(type);
            }
        }

        return result.ToArray();
    }

}
