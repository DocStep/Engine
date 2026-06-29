using System.Numerics;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Engine.Graphics;

namespace Engine;


public class Scene {
    public Scene () {

    }

    public virtual void Load () {
        
    }

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


}
