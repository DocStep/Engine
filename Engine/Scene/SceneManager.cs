using System.Diagnostics;
using Engine.Input;

namespace Engine;


public class SceneManager : Singleton<SceneManager> {

    public readonly List<Scene> Scenes = new List<Scene>();
    private readonly int sceneActiveID = 0;

    public static Scene? ActiveScene {
        get {
            if (0 < Instance.Scenes.Count) return Instance.Scenes[Instance.sceneActiveID];
            else {
                Log.log(error_NoActiveScene, LogType.warning);
                //Log.log(new StackTrace());
                return null;
            }
        }
    }
    public static int ActiveSceneID => Instance.sceneActiveID;

    public const string error_NoActiveScene = "No active scene found.";


    protected override void Init () {
        Engine.Instance.de_Update += Update;
    }

    public void Update () {
        if (ArgsUtils.isEditor && Inputs.Actions[Inputs.EditorSave].pressedDown) {
            ActiveSceneSave();
        }

        //int count = Scenes.Count;
        //for (int i = 0; i < count; i++) {
        //    Scenes[i].Update();
        //}
    }


    public void ActiveSceneSave () {
        if (!ArgsUtils.isEditor) return;
        Project.SaveActiveScene();
    }


    /// Drops every loaded scene and destroys its objects. Safe to call before loading another scene.
    public void UnloadAll () {
        List<Scene> loaded = new List<Scene>(Scenes);
        foreach (Scene scene in loaded) {
            List<GameObject> objects = new List<GameObject>(scene.GameObjects);
            for (int i = objects.Count - 1; i >= 0; i--) {
                if (!objects[i].Destroyed) objects[i].DestroyImmediate();
            }
        }
        Scenes.Clear();
    }

}
