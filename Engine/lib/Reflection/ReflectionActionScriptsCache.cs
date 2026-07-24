namespace Engine;


internal static class ReflectionActionScriptsCache<T> {

    private static List<T> scriptsCache = new List<T>();


    public static List<T> FindAll (ReflectionActionScripts reflection) {
        if (scriptsCache is not null) return scriptsCache;

        scriptsCache = new List<T>(0);
        foreach (IActionScript script in reflection.Scripts) {
            if (script is T type) {
                scriptsCache.Add(type);
            }
        }

        /// Priority
        scriptsCache.Sort();

        return scriptsCache;
    }

}
