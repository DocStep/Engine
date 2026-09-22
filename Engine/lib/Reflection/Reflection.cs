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

    public static MemberInfo[] FindAllMembers (object obj, bool includeInherited = true, bool includeNonPublic = true) {
        Type type = obj is Type t ? t : obj.GetType();
        return FindAllMembers(type, includeInherited, includeNonPublic);
    }
    public static MemberInfo[] FindAllMembers (Type type, bool includeNonPublic = true) {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
        if (includeNonPublic) flags |= BindingFlags.NonPublic;
        //if (!includeInherited) flags |= BindingFlags.DeclaredOnly;

        List<MemberInfo> result = new List<MemberInfo>();
        result.AddRange(type.GetFields(flags));
        result.AddRange(type.GetProperties(flags));

        return result.ToArray();
    }

    public static List<T> GetAllMaterials<T> (Type type) {
        List<T> result = new List<T>();

        BindingFlags flags = BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        foreach (FieldInfo field in type.GetFields(flags)) {
            if (field.FieldType != typeof(T)) continue;

            T? value = (T?)field.GetValue(null);
            if (value is null) continue;

            result.Add(value);
        }

        return result;
    }

}
