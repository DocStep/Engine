namespace Engine;


public interface IAsset : IDisposable { }


/// Loads, caches, and disposes engine assets by path
public static class AssetsManager {
    static readonly Dictionary<string, IAsset> _cache = new Dictionary<string, IAsset>();
    static readonly Dictionary<Type, Func<string, IAsset>> _loaders = new Dictionary<Type, Func<string, IAsset>>();

    public static void Init () {
        AssetsManager.Register<Graphics.Mesh>(path => new Graphics.Mesh(Graphics.ObjLoader.Load(path)));
    }

    /// Registers how to construct an asset type from a path, call once at startup per type
    public static void Register<T> (Func<string, T> loader) where T : class, IAsset {
        _loaders[typeof(T)] = path => loader(path);
    }

    /// Gets an already-loaded asset, or loads and caches it using the registered loader
    public static T Get<T> (string path) where T : class, IAsset {
        string key = CacheKey<T>(path);

        if (_cache.TryGetValue(key, out IAsset? existing))
            return (T)existing;

        if (!_loaders.TryGetValue(typeof(T), out Func<string, IAsset>? loader))
            throw new InvalidOperationException("No loader registered for type " + typeof(T).Name);

        IAsset asset = loader(path);
        _cache[key] = asset;
        return (T)asset;
    }

    /// Unloads and disposes a single asset
    public static void Unload<T> (string path) where T : class, IAsset {
        string key = CacheKey<T>(path);

        if (_cache.TryGetValue(key, out IAsset? asset)) {
            asset.Dispose();
            _cache.Remove(key);
        }
    }

    /// Disposes and clears every cached asset, e.g. on engine shutdown
    public static void UnloadAll () {
        foreach (IAsset asset in _cache.Values)
            asset.Dispose();

        _cache.Clear();
    }

    static string CacheKey<T> (string path) {
        return typeof(T).Name + ":" + path;
    }
}
