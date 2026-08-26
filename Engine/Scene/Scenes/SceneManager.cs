using Engine.Input;

namespace Engine;


public class SceneManager : Singleton<SceneManager> {

    public readonly List<Scene> scenes = new List<Scene>();
    private readonly int sceneActiveID = 0;

    public static Scene ActiveScene => Instance.scenes[Instance.sceneActiveID];
    public static int ActiveSceneID => Instance.sceneActiveID;


    protected override void Init () {
        Scene scene;
        //scene = new SceneMaterials() { Name = "Scene Materials", };
        //scene = new ScenePhysics() { Name = "Scene Physics", };
        //scene = new SceneTerrain() { Name = "Scene Terrain", };
        scene = new SceneUI() { Name = "Scene UI", };

        Engine.Instance.de_Update += Update;
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
