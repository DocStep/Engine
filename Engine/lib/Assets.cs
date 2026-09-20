using RawFont = Engine.Graphics.UI.RawFont;
using FontAtlas = Engine.Graphics.UI.FontAtlas;

namespace Engine;


/// Loads, caches, and disposes engine assets by path
public static class Assets {
    static readonly Dictionary<string, IAsset> _cache = new Dictionary<string, IAsset>();
    static readonly Dictionary<string, int> _refCount = new Dictionary<string, int>();
    public const string DefaultFontPath = "src/Fonts/FuturaCyrillicMedium.ttf";

    static readonly Dictionary<string, FontAtlas> _fontCache = new Dictionary<string, FontAtlas>();
    static readonly Dictionary<string, int> _fontRefCount = new Dictionary<string, int>();

    static string NormalizePath (string path) {
        return Path.GetFullPath(path).ToLowerInvariant();
    }

    static string CacheKey<T> (string path) {
        return typeof(T).Name + ":" + path;
    }

    static string FontCacheKey (string path, float fontSize) {
        return path + ":" + fontSize;
    }

    public static T Load<T> (string path) where T : class, IAsset<T> {
        path = NormalizePath(path);
        string key = CacheKey<T>(path);

        if (_cache.TryGetValue(key, out IAsset? existing)) {
            _refCount[key]++;
            return (T)existing;
        }

        T asset;
        try {
            asset = T.Load(path);
        } catch (Exception e) {
            throw new IOException($"Failed to load {typeof(T).Name} from '{path}': {e.Message}.", e);
        }

        _cache[key] = asset;
        _refCount[key] = 1;
        return asset;
    }

    public static string LoadText (string relativePath) {
        string fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {fullPath}.");
        return File.ReadAllText(fullPath);
    }

    /// Loads and caches a font atlas for a given file + size pair; the underlying raw font
    /// bytes are cached separately (via Load<RawFont>), so baking the same file at another
    /// size skips the disk read
    public static FontAtlas LoadFont (string path, float fontSize) {
        path = NormalizePath(path);
        string key = FontCacheKey(path, fontSize);

        if (_fontCache.TryGetValue(key, out FontAtlas? existing)) {
            _fontRefCount[key]++;
            return existing;
        }

        RawFont font = Load<RawFont>(path);
        FontAtlas atlas;
        try {
            atlas = FontAtlas.Load(font, fontSize);
        } catch (Exception e) {
            Unload<RawFont>(path); /// undo the ref bump above since the atlas never got cached
            throw new IOException($"Failed to bake FontAtlas from '{path}' at size {fontSize}: {e.Message}.", e);
        }

        _fontCache[key] = atlas;
        _fontRefCount[key] = 1;
        return atlas;
    }

    /// Releases a reference to an asset; disposes it once no references remain
    public static void Unload<T> (string path) where T : class, IAsset<T> {
        path = NormalizePath(path);
        string key = CacheKey<T>(path);

        if (!_cache.TryGetValue(key, out IAsset? asset)) return;
        if (--_refCount[key] > 0) return;

        asset.Dispose();
        _cache.Remove(key);
        _refCount.Remove(key);
    }

    /// Releases a reference to a font atlas; disposes it once no references remain.
    /// Also releases the underlying RawFont reference taken by LoadFont
    public static void UnloadFont (string path, float fontSize) {
        path = NormalizePath(path);
        string key = FontCacheKey(path, fontSize);

        if (!_fontCache.TryGetValue(key, out FontAtlas? atlas)) return;
        if (--_fontRefCount[key] > 0) return;

        atlas.Dispose();
        _fontCache.Remove(key);
        _fontRefCount.Remove(key);

        Unload<RawFont>(path);
    }

    /// Disposes and clears every cached asset, e.g. on engine shutdown
    public static void UnloadAll () {
        foreach (FontAtlas atlas in _fontCache.Values)
            atlas.Dispose();
        _fontCache.Clear();
        _fontRefCount.Clear();

        foreach (IAsset asset in _cache.Values)
            asset.Dispose();
        _cache.Clear();
        _refCount.Clear();
    }

}