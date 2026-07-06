namespace Engine;


public static class Dirs {

    public static readonly string Root = AppContext.BaseDirectory;
    public static readonly string Assets = Path.Combine(Root, "src");
    public static readonly string Shaders = Path.Combine(Assets, "Shaders");
    public static readonly string Textures = Path.Combine(Assets, "Textures");
    public static readonly string Models = Path.Combine(Assets, "Models");
    public static readonly string Fonts = Path.Combine(Assets, "Fonts");
    public static readonly string Scenes = Path.Combine(Assets, "Scenes");

    /// Ensures all known directories exist on disk
    public static void EnsureExist () {
        EnsureExist(Assets);
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
    public static string Resolve (string baseDir, string relativePath) {
        return Path.GetFullPath(Path.Combine(baseDir, relativePath));
    }

}
