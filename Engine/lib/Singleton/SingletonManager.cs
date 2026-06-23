using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine;


public class SingletonManager {

    //public static HashSet<ISingleton> Singletons = new HashSet<ISingleton>();
    public static HashSet<ISingleton> Singletons = new HashSet<ISingleton>();

    public static readonly object _lock = new object();


    public static void Add (ISingleton singleton) {
        lock (_lock)
            Singletons.Add(singleton);
    }
    public static void Add_NoLock (ISingleton singleton) {
        Singletons.Add(singleton);
    }
    public static void Remove (ISingleton singleton) {
        lock (_lock)
            Singletons.Remove(singleton);
    }

    public static void ClearAll () {
        lock (_lock) {
            foreach (var singleton in Singletons) {
                singleton.Clear();
            }
            Singletons.Clear();
        }
    }


    /*public static void DisposeAll () {
        foreach (var singleton in Singletons) {
            singleton.Dispose_();
        }
    }*/

}
