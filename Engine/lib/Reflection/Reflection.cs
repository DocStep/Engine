using System.Reflection;
using System.Linq;

namespace Engine;


public class Reflection : Singleton<Reflection> {

    public static Type[] FindAllSubclasses<T> (bool doAbstract = false) {
        Type baseType = typeof(T);
        Assembly? assembly = Assembly.GetAssembly(baseType);
        if (assembly is null) return [];

        Type[] types = assembly.GetTypes();
        Type[] subClasses = types.Where(type => type.IsSubclassOf(baseType) && (doAbstract || !type.IsAbstract)).ToArray();
        return subClasses;
    }

}
