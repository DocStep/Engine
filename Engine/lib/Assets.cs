namespace Engine;


public interface IAsset : IDisposable {
    public string Name { get; }
}

/// Asset type that knows how to load itself from a path
public interface IAsset<T> : IAsset where T : IAsset<T> {
    static abstract T Load (string path);
}


/// Loads, caches, and disposes engine assets by path
public static class Assets {
    static readonly Dictionary<string, IAsset> _cache = new Dictionary<string, IAsset>();

    static string CacheKey<T> (string path) {
        return typeof(T).Name + ":" + path;
    }

    public static T Load<T> (string path) where T : class, IAsset<T> {
        path = Path.GetFullPath(path);
        string key = CacheKey<T>(path);

        if (_cache.TryGetValue(key, out IAsset? existing))
            return (T)existing;

        T asset = T.Load(path);
        _cache[key] = asset;
        return asset;
    }

    /// Unloads and disposes a single asset
    public static void Unload<T> (string path) where T : class, IAsset<T> {
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
}