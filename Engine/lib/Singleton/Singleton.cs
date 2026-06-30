using System;
using System.Collections.Generic;
using System.Text;

namespace Engine;


public class Singleton<T> : ISingleton where T : Singleton<T>, new() {

    public const string Name = "Singleton";
    protected readonly bool debugLog = true;

    private static T instance = null!;
    public static T Instance {
        get {
            if (instance is null) {
                lock (SingletonManager._lock) {
                    if (instance is null) 
                        InstanceNewRaw_NoLock();
                    return instance!;
                }
            }
            return instance;
        }
    }


    protected virtual void Init () { }


    public void Clear () {
        lock (SingletonManager._lock)
            instance = null!;
    }
    public void ReInstanceNew () {
        Log.log($"[{Name}] ReInstanceNew: {typeof(T)}");
        InstanceNew();
    }
    //protected virtual void Dispose () { }

    public static bool HasSingleton () {
        lock (SingletonManager._lock)
            return instance is not null;
    }
    public static bool HasSingleton_NoLock () {
        return instance is not null;
    }

    /*public static void InstanceNull () {
        if (instance is null) InstanceNewRaw();
    }*/
    public static void InstanceNew () {
        lock (SingletonManager._lock) {
            if (instance is not null) SingletonManager.Remove(instance);
            InstanceNewRaw_NoLock();
        }
    }
    /*private static void InstanceNewRaw () {
        lock (SingletonManager._lock) {
            InstanceNewRaw_NoLock();
        }
    }*/
    private static void InstanceNewRaw_NoLock () {
        instance = new T();
        instance.Init();
        SingletonManager.Add_NoLock(instance);

        if (instance.debugLog) {
            string text = $"[{Name}] Inited: {typeof(T)} ({Instance.GetHashCode()})";
            Log.log(text);
        }
    }


    /*public void Dispose_ () {
        Dispose();
        instance = null;

        Log.log($"[{Name}] Disposed: {GetType()}");
    }
    public static void DisposeAll () {
        foreach (var singleton in SingletonManager.Singletons) {
            singleton.Dispose_();
        }
    }*/

}
