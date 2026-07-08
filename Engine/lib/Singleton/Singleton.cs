using System.Diagnostics;

namespace Engine;


public class Singleton<T> : ISingleton where T : Singleton<T>, new() {
    public Singleton () {
        lock (SingletonManager._lock) {
            if (instance is not null) 
                throw new InvalidOperationException($"[{Name}] {instance} ({instance.GetHashCode()}<-{GetHashCode()}) already has an active instance. " +
                    $"Use InstanceNew() to replace it, or Instance to access it.");

            instance = (T)this;
            instance.Init();
            SingletonManager.Add_NoLock(instance);

            if (instance.debugLog) {
                string text = $"[{Name}] Inited: {typeof(T)} ({instance.GetHashCode()})";
                Log.log(text);
            }
        }
    }


    public const string Name = "Singleton";
    protected readonly bool debugLog = true;

    private static T instance = null!;
    public static T Instance {
        get {
            if (instance is null) {
                lock (SingletonManager._lock) {
                    if (instance is null) new T();
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

    public static bool HasSingleton () {
        lock (SingletonManager._lock)
            return instance is not null;
    }
    public static bool HasSingleton_NoLock () {
        return instance is not null;
    }

    public static void InstanceCheck () {
        lock (SingletonManager._lock) {
            if (instance is null)
                _ = new T();
        }
    }
    public static void InstanceNew () {
        lock (SingletonManager._lock) {
            if (instance is not null) {
                Log.log($"[{Name}] ReInstanceNew: {typeof(T)}");
                SingletonManager.Remove(instance);
                instance = null!;
            }
            _ = new T();
        }
    }

}