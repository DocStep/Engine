using Engine.Input;

namespace Engine;


public class SceneManager : Singleton<SceneManager> {

    public readonly List<Scene> scenes = new List<Scene>();
    private readonly int sceneActiveID = 0;

    public static Scene? ActiveScene {
        get {
            if (0 < Instance.scenes.Count) return Instance.scenes[Instance.sceneActiveID];
            else return null;
        }
    }
    public static int ActiveSceneID => Instance.sceneActiveID;


    protected override void Init () {
        Engine.Instance.de_Update += Update;
    }

    public void Awake () {
        Scene scene;
        //scene = new SceneMaterials() { Name = "Scene Materials", };
        //scene = new ScenePhysics() { Name = "Scene Physics", };
        //scene = new SceneTerrain() { Name = "Scene Terrain", };
        //scene = new SceneUI() { Name = "Scene UI", };
        scene = new SceneChunksGrid() { Name = "Scene Chunks Grid", };
        //scene.Save("src/Scenes/SceneChunksGrid.json");
        //scene = Assets.Load<Scene>("src/Scenes/SceneChunksGrid.json");
        //scene = new ScenePrefabs() { Name = "Scene Prefabs", };
    }

    public void Update () {
        if (Inputs.Actions[Inputs.EditorSave].pressedDown) {
            ActiveSceneSave();
        }

        int count = scenes.Count;
        for (int i = 0; i < count; i++) {
            scenes[i].Update();
        }
    }


    public void ActiveSceneSave () {
        Scene activeScene = ActiveScene;
        JObj jObj = activeScene.ToJObj();
        string path = Path.Combine(Dirs.Scenes, activeScene.Name + ".json");

        Dirs.EnsureExist(Dirs.Scenes);
        Json.Write(path, jObj);
    }

}
