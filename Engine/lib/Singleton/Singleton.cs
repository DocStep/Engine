public abstract class Singleton<T> : ISingleton where T : Singleton<T>, new() {

    public const string Name = "Singleton";
    protected readonly bool debugLog = true;

    private static T? instance = null;
    public static T Instance {
        get {
            lock (SingletonManager._lock) {
                if (instance is null) throw new Exception($"[{Name}] {typeof(T)}.{nameof(Instance)} is null");
                return instance;
            }
        }
    }

    /// Creates a new T, registers it as the singleton, throws if one already exists
    public static T CreateSingleton () {
        lock (SingletonManager._lock) {
            if (instance is not null)
                throw new InvalidOperationException($"[{Name}] {typeof(T)} already has an active instance ({instance.GetHashCode()}). Use InstanceNew() to replace it, or Instance to access it.");

            T obj = new T();
            instance = obj;

            if (obj.debugLog)
                Log.log($"[{Name}] Inited: {typeof(T)} ({obj.GetHashCode()})");

            obj.Init();
            return instance;
        }
    }
    public static void InstanceNew () {
        lock (SingletonManager._lock) {
            if (instance is not null) {
                Log.log($"[{Name}] ReInstanceNew: {typeof(T)}");
                SingletonManager.Remove(instance);
                instance = null!;
            }
            CreateSingleton();
        }
    }
    /*public static void InstanceCheck () {
        lock (SingletonManager._lock) {
            if (instance is null)
                CreateSingleton();
        }
    }*/


    protected virtual void Init () { }


    public static bool HasSingleton () {
        lock (SingletonManager._lock)
            return instance is not null;
    }
    public static bool HasSingleton_NoLock () {
        return instance is not null;
    }
    public void Clear () {
        lock (SingletonManager._lock) {
            if (instance == this)
                instance = null!;
        }
    }

}