using Newtonsoft.Json;

namespace Engine;


public class Scene : ISavable, IUpdate {
    public Scene () {
        Name = GetType().Name;
        SceneManager.Instance.scenes.Add(this);
        OnCreate();
    }
    /*public Scene (string path) {
        Name = GetType().Name;
        SceneManager.Instance.scenes.Add(this);
        Load();
    }*/

    [JsonIgnore] public readonly static string TypeName = typeof(Scene).Name;
    [JsonIgnore] public bool Enabled { get; set; } = true;

    public string Name = nameof(Scene);
    protected readonly List<GameObject> objects = new List<GameObject>();
    public List<GameObject> GameObjects => objects;



    public void ObjectAdd (GameObject gameObject) {
        objects.Add(gameObject);
    }
    /*public void ObjectRemove (GameObject gameObject) {
        objects.Remove(gameObject);
    }*/


    public virtual void OnCreate () {

    }

    public void Update () {

    }


    public GameObject? Find (string name) {
        for (int i = 0; i < objects.Count; i++) {
            if (objects[i].Name == name)
                return objects[i];
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

        int count = GameObjects.Count;
        for (int i = 0; i < count; i++) {
            GameObjects[i].PreSave();
        }
    }

    public void PostLoad () {
        
    }

    void IUpdate.Update () {
        throw new NotImplementedException();
    }
}
