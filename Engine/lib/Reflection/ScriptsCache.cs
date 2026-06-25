using System;
using System.Collections.Generic;
using System.Text;

namespace Engine;


public static class ScriptsCache<T> {

    private static List<T> Scripts = new List<T>();


    public static List<T> FindAll (ReflectionScripts reflection) {
        if (Scripts is not null) return Scripts;

        Scripts = new List<T>(0);
        foreach (var script in reflection.Scripts) {
            if (script is T type) {
                Scripts.Add(type);
            }
        }

        /// Priority
        Scripts.Sort();

        return Scripts;
    }

}
