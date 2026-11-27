namespace Engine;


public static class Project {

    public static void SaveActiveScene () {
        Scene? scene = SceneManager.ActiveScene;
        if (scene == null) return;

        string path = scene.Path ?? Path.Combine(Dirs.Scenes, $"{scene.Name}.json");
        Dirs.EnsureExist(Dirs.Scenes);
        scene.Save(path);
    }

    public static void RenameSceneFile (Scene scene) {
        string? oldPath = scene.Path is null ? null : Path.GetFullPath(scene.Path);
        string directory = oldPath is null ? Dirs.Scenes : Path.GetDirectoryName(oldPath) ?? Dirs.Scenes;
        string extension = oldPath is null ? ".json" : Path.GetExtension(oldPath);
        if (string.IsNullOrEmpty(extension)) extension = ".json";

        if (string.IsNullOrWhiteSpace(scene.Name)
            || scene.Name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) {
            Log.log($"Invalid scene name '{scene.Name}'.", LogType.warning);
            scene.Name = oldPath is null ? "New Scene" : Path.GetFileNameWithoutExtension(oldPath);
            return;
        }

        string newPath = Path.Combine(directory, scene.Name + extension);
        if (oldPath is not null && string.Equals(Path.GetFullPath(oldPath), Path.GetFullPath(newPath), StringComparison.OrdinalIgnoreCase))
            return;

        if (File.Exists(newPath)) {
            Log.log($"Cannot rename scene: '{newPath}' already exists.", LogType.warning);
            scene.Name = oldPath is null ? "New Scene" : Path.GetFileNameWithoutExtension(oldPath);
            return;
        }

        Dirs.EnsureExist(directory);
        if (oldPath is null) scene.Save(newPath);
        else File.Move(oldPath, newPath);
        scene.Path = Path.GetFullPath(newPath);
    }

}
