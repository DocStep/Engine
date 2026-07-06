using Newtonsoft.Json;

namespace Engine;


public class Scene : ISavable {
    public Scene () {
        Name = GetType().Name;
        SceneManager.Instance.scenes.Add(this);
        Load();
    }
    /*public Scene (string path) {
        Name = GetType().Name;
        SceneManager.Instance.scenes.Add(this);
        Load();
    }*/

    [JsonIgnore] public readonly static string typeName = typeof(Scene).Name;



    public virtual void Load () {
        
    }

    public string Name = "Scene";
    protected readonly List<GameObject> objects = new();
    public List<GameObject> Objects => objects;
    public void ObjectAdd (GameObject gameObject) {
        objects.Add(gameObject);
    }
    /*public void ObjectRemove (GameObject gameObject) {
        objects.Remove(gameObject);
    }*/

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
        return new JObj(typeName, this);
    }

    public void PreSave () {
        /// Own
        /// ...

        int count = Objects.Count;
        for (int i = 0; i < count; i++) {
            Objects[i].PreSave();
        }
    }

    public void PostLoad () {
        
    }
}
