using Newtonsoft.Json;

namespace Engine;


public class Scene : IAsset<Scene> {
    public Scene () {
        Name = GetType().Name;
        SceneManager.Instance.Scenes.Add(this);
        OnGenerate();
    }
    [JsonConstructor]
    internal Scene (bool deserializing) {
        //Name = GetType().Name;
        //SceneManager.Instance.scenes.Add(this);
    }

    /*public Scene (string path) {
        Name = GetType().Name;
        SceneManager.Instance.scenes.Add(this);
        Load();
    }*/

    [JsonIgnore] public readonly static string TypeName = typeof(Scene).Name;
    [JsonIgnore] public bool Enabled { get; set; } = true;

    public string Name { get; set; } = nameof(Scene);
    public long Id { get; set; }
    public string? Path { get; set; }

    [JsonProperty] protected readonly List<GameObject> Objects = new List<GameObject>();
    public List<GameObject> GameObjects => Objects;



    public void GameObjectAdd (GameObject gameObject) {
        Objects.Add(gameObject);
        //Log.log("GameObjectAdd", gameObject.Name);
    }
    /*public void ObjectRemove (GameObject gameObject) {
        objects.Remove(gameObject);
    }*/


    public virtual void OnGenerate () {

    }



    public GameObject? Find (string name) {
        for (int i = 0; i < Objects.Count; i++) {
            if (Objects[i].Name == name)
                return Objects[i];
        }
        return null;
    }



    public virtual void DrawRaw () {

    }


    public JObj ToJObj () {
        PreSave();
        return new JObj(TypeName, this);
    }

    public void PreSave () {
        /// Own
        /// ...

        /*int count = GameObjects.Count;
        for (int i = 0; i < count; i++) {
            GameObjects[i].PreSave();
        }*/
    }
    public void PostLoad () {
        
    }


    public void Save (string path) {
        Path = path;
        Prefab.SaveObjects(Objects, path);
    }
    public static Scene Create (string path) {
        string fullPath = System.IO.Path.GetFullPath(path);
        string? directory = System.IO.Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        Scene scene = new Scene(true) {
            Path = fullPath,
            Name = System.IO.Path.GetFileNameWithoutExtension(fullPath),
        };
        scene.Save(fullPath);
        return scene;
    }

    public static Scene? Load (string path, int part = 100) {
        if (!System.IO.Path.HasExtension(path) && File.Exists(path + ".json")) path += ".json";
        if (!File.Exists(path)) return null;

        /// The new scene has to be the active one before objects are read, because
        /// Prefab registers each GameObject with SceneManager.ActiveScene.
        SceneManager.Instance.UnloadAll();
        Scene scene = new Scene(true) {
            Path = path,
            Name = System.IO.Path.GetFileNameWithoutExtension(path),
        };
        SceneManager.Instance.Scenes.Add(scene);
        Prefab.LoadObjects(path);
        return scene;
    }


    public void Dispose () {

    }
}
