using System.Numerics;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Engine.Graphics;

namespace Engine;


public static class SceneManager {

    private static readonly List<Scene> scenes = new List<Scene>();
    private static readonly int sceneActiveID = 0;

    public static Scene ActiveScene => scenes[sceneActiveID];
    public static int ActiveSceneID => sceneActiveID;


    public static void Init () {
        Scene scene;
        scene = new SceneMaterials();
        //scene = new ScenePhysics();
        scenes.Add(scene);
        scene.Load();


        Engine.Instance.de_Update += Update;
    }


    public static void Update () {
        int count = scenes.Count;
        for (int i = 0; i < count; i++) {
            scenes[i].Update();
        }
    }


}
