namespace Engine;


public static class Dirs {

    private static string? projectRoot;
    public static string Root { get; } = AppContext.BaseDirectory;
    public static string ProjectRoot => projectRoot ??= FindProjectRoot();
    public static string AssetsPath { get; } = System.IO.Path.Combine(Root, "src");
    public static string Shaders { get; } = System.IO.Path.Combine(AssetsPath, "Shaders");
    public static string Textures { get; } = System.IO.Path.Combine(AssetsPath, "Textures");
    public static string Models { get; } = System.IO.Path.Combine(AssetsPath, "Models");
    public static string Fonts { get; } = System.IO.Path.Combine(AssetsPath, "Fonts");
    public static string Scenes {
        get {
            string projectScenes = System.IO.Path.Combine(ProjectRoot, "Scenes");
            if (Directory.Exists(projectScenes)) return projectScenes;

            string gameScenes = System.IO.Path.Combine(ProjectRoot, "Game", "Scenes");
            if (Directory.Exists(gameScenes)) return gameScenes;

            return System.IO.Path.Combine(AssetsPath, "Scenes");
        }
    }
    public static string Data {
        get {
            string projectData = System.IO.Path.Combine(ProjectRoot, "Data");
            if (Directory.Exists(projectData)) return projectData;

            string parentData = System.IO.Path.Combine(Directory.GetParent(ProjectRoot)?.FullName ?? ProjectRoot, "Data");
            if (Directory.Exists(parentData)) return parentData;

            return System.IO.Path.Combine(Root, "Data");
        }
    }

    public static void InitializeProjectRoot (string? path) {
        if (string.IsNullOrWhiteSpace(path)) return;

        string fullPath = System.IO.Path.GetFullPath(path);
        if (!Directory.Exists(fullPath))
            throw new DirectoryNotFoundException($"Project directory does not exist: {fullPath}");
        projectRoot = fullPath;
    }

    private static string FindProjectRoot () {
        string[] startPaths = { Environment.CurrentDirectory, Root };
        foreach (string startPath in startPaths) {
            for (DirectoryInfo? directory = new(startPath); directory is not null; directory = directory.Parent) {
                if (File.Exists(System.IO.Path.Combine(directory.FullName, "Game.csproj")))
                    return directory.FullName;
                string gameProject = System.IO.Path.Combine(directory.FullName, "Game", "Game.csproj");
                if (File.Exists(gameProject))
                    return System.IO.Path.GetDirectoryName(gameProject)!;
            }
        }

        return Root;
    }

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
