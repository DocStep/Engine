namespace Engine;


public static class Project {

    public static void SaveActiveScene () {
        Scene? scene = SceneManager.ActiveScene;
        if (scene == null) return;

        string path = scene.Path ?? Path.Combine(Dirs.Scenes, $"{scene.Name}.json");
        Dirs.EnsureExist(Dirs.Scenes);
        scene.Save(path);
    }

}