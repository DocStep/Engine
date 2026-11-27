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

    /// Built-ins (e.g. AssetsEngine._mat_Lit, _mesh_Cube) never touch disk. Register them once at
    /// startup under a "builtin:Name" key so Assets.Load<T> can hand back the exact same instance
    /// instead of trying to resolve a path — same identity story as a file-backed asset.
    static readonly Dictionary<string, IAsset> _builtins = new Dictionary<string, IAsset>();

    static string CacheKey<T> (string path) {
        return typeof(T).Name + ":" + path;
    }

    static string FontCacheKey (string path, float fontSize) {
        return path + ":" + fontSize;
    }

    /// Normalizes to (fullPath, relativePath) regardless of which one the caller passed in —
    /// fullPath is only ever used for the actual file read, relativePath is what gets stored on
    /// the asset and used as the cache key, so identity stays portable across machines.
    static (string fullPath, string relativePath) ResolvePath (string path) {
        if (Path.IsPathRooted(path)) {
            return (path, Path.GetRelativePath(AppContext.BaseDirectory, path));
        }
        return (Path.Combine(AppContext.BaseDirectory, path), path);
    }

    static string[] GetExtensions (Type assetType) {
        if (assetType == typeof(Graphics.Mesh)) return [".fbx", ".obj"];
        if (assetType == typeof(Graphics.Material)) return [".mat"];
        if (assetType == typeof(Graphics.Shader)) return [".shader"];
        if (assetType == typeof(Graphics.Texture)) return [".png", ".jpg", ".jpeg", ".bmp", ".tga"];
        if (assetType == typeof(Graphics.HdrTexture)) return [".hdr", ".hdrf"];
        if (assetType == typeof(Graphics.ShaderRaw)) return [".shader", ".glsl", ".vert", ".frag"];
        if (assetType == typeof(Graphics.UI.RawFont)) return [".ttf"];
        if (assetType == typeof(Audio.AudioClip)) return [".wav"];
        if (assetType == typeof(Scene) || assetType == typeof(GameObject)) return [".json"];
        return [];
    }

    static (string fullPath, string relativePath) ResolvePath (Type assetType, string path) {
        (string fullPath, _) = ResolvePath(path);
        if (assetType == typeof(Graphics.Mesh)) fullPath = Graphics.Mesh.ResolveSourcePath(fullPath);
        else if (!Path.HasExtension(fullPath)) {
            foreach (string extension in GetExtensions(assetType)) {
                string candidate = fullPath + extension;
                if (File.Exists(candidate)) {
                    fullPath = candidate;
                    break;
                }
            }
        }
        return (fullPath, Path.GetRelativePath(AppContext.BaseDirectory, fullPath));
    }

    public static bool SupportsAssetPath (Type assetType, string path) {
        string extension = Path.GetExtension(path);
        return GetExtensions(assetType).Contains(extension, StringComparer.OrdinalIgnoreCase)
            || (assetType == typeof(Graphics.Mesh) && (extension.Equals(".obj", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".fbx", StringComparison.OrdinalIgnoreCase)));
    }

    public static void RegisterBuiltin<T> (string name, T asset) where T : class, IAsset<T> {
        asset.Path = "builtin:" + name;
        _builtins[asset.Path] = asset;
    }

    public static void SaveAll () {
        //Type[] types = Reflection.FindAllSubclasses<IAsset>(doAbstract: false);
    }


    public static T? Load<T> (string path) where T : class, IAsset<T> {
        if (_builtins.TryGetValue(path, out IAsset? builtin)) return (T)builtin;

        (string fullPath, string relativePath) = ResolvePath(typeof(T), path);
        string key = CacheKey<T>(relativePath);

        if (_cache.TryGetValue(key, out IAsset? existing)) {
            _refCount[key]++;
            return (T)existing;
        }

        T? asset;
        try {
            asset = T.Load(fullPath);
            if (asset is null) return null;
            asset.Path = relativePath;
        } catch (Exception) {
            throw;
            //throw new Exception($"Failed to load {typeof(T).Name} from '{path}': {e.Message}.", e);
        }

        _cache[key] = asset;
        _refCount[key] = 1;
        return asset;
    }

    public static string LoadText (string relativePath) {
        (string fullPath, _) = ResolvePath(relativePath);
        if (!Path.HasExtension(fullPath)) {
            foreach (string extension in new[] { ".shader", ".vert", ".frag", ".glsl" }) {
                string candidate = fullPath + extension;
                if (File.Exists(candidate)) {
                    fullPath = candidate;
                    break;
                }
            }
        }
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {fullPath}.");
        return File.ReadAllText(fullPath);
    }

    /// Loads and caches a font atlas for a given file + size pair; the underlying raw font
    /// bytes are cached separately (via Load<RawFont>), so baking the same file at another
    /// size skips the disk read
    public static FontAtlas LoadFont (string path, float fontSize) {
        string key = FontCacheKey(path, fontSize);

        if (_fontCache.TryGetValue(key, out FontAtlas? existing)) {
            _fontRefCount[key]++;
            return existing;
        }

        RawFont font = Load<RawFont>(path)!;
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
        if (_builtins.ContainsKey(path)) return; /// built-ins are never disposed

        (_, string relativePath) = ResolvePath(typeof(T), path);
        string key = CacheKey<T>(relativePath);

        if (!_cache.TryGetValue(key, out IAsset? asset)) return;
        if (--_refCount[key] > 0) return;

        asset.Dispose();
        _cache.Remove(key);
        _refCount.Remove(key);
    }

    /// Releases a reference to a font atlas; disposes it once no references remain.
    /// Also releases the underlying RawFont reference taken by LoadFont
    public static void UnloadFont (string path, float fontSize) {
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
