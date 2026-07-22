namespace Engine;


public static class Dirs {

    public static readonly string Root = AppContext.BaseDirectory;
    public static readonly string AssetsPath = System.IO.Path.Combine(Root, "src");
    public static readonly string Shaders = System.IO.Path.Combine(AssetsPath, "Shaders");
    public static readonly string Textures = System.IO.Path.Combine(AssetsPath, "Textures");
    public static readonly string Models = System.IO.Path.Combine(AssetsPath, "Models");
    public static readonly string Fonts = System.IO.Path.Combine(AssetsPath, "Fonts");
    public static readonly string Scenes = System.IO.Path.Combine(AssetsPath, "Scenes");

    /// Ensures all known directories exist on disk
    public static void EnsureExist () {
        EnsureExist(AssetsPath);
        EnsureExist(Shaders);
        EnsureExist(Textures);
        EnsureExist(Models);
        EnsureExist(Fonts);
        EnsureExist(Scenes);
    }
    public static void EnsureExist (string path) {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
    }

    /// Resolves a path relative to a base folder, e.g. Resolve(Textures, "player.png")
    public static string Path (string baseDir, string relativePath) {
        return System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, relativePath));
    }

}
