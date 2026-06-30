using System.Numerics;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Engine.Graphics;

namespace Engine;


public class SceneManager : Singleton<SceneManager> {

    private readonly List<Scene> scenes = new List<Scene>();
    private readonly int sceneActiveID = 0;

    public static Scene ActiveScene => Instance.scenes[Instance.sceneActiveID];
    public static int ActiveSceneID => Instance.sceneActiveID;


    protected override void Init () {
        Scene scene;
        //scene = new SceneMaterials();
        //scene = new ScenePhysics();
        scene = new SceneGizmos();
        scenes.Add(scene);
        scene.Load();

        Engine.Instance.de_Update += Update;
    }


    public void Update () {
        int count = scenes.Count;
        for (int i = 0; i < count; i++) {
            scenes[i].Update();
        }
    }


}
