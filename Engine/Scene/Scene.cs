using Newtonsoft.Json;

namespace Engine;


public class Scene : IUpdate, IAsset<Scene> {
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

    public void Update () {

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
    public static Scene? Load (string path) {
        Scene scene = new Scene(true) { Path = path };
        scene.Objects.Clear(); /// constructor left Objects empty anyway, but explicit if that changes
        scene.Objects.AddRange(Prefab.LoadObjects(path));
        SceneManager.Instance.Scenes.Add(scene);
        return scene;
    }


    public void Dispose () {

    }
}
