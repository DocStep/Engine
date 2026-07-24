using Newtonsoft.Json;

namespace Engine;


public abstract class Component : ISavable {
    public Component () { }

    public readonly Guid? Guid = lib.Guid;
    public readonly long? Id = lib.Id;

    [JsonIgnore] public GameObject owner = null!;
    public Guid? ownerGuid = null;
    [JsonIgnore] public TransformComponent? parent = null;
    public Guid? parentGuid = null;
    public abstract string Name { get; }


    //protected string? typeName = null;


    public virtual void SetParent (GameObject gameObject) {
        owner = gameObject;
    }

    public virtual void OnAdd () { }
    public virtual void OnRemove () { }


    public virtual void DrawInspector () { }


    public void PreSave () {
        /// Own
        /// ...
    }
    //public abstract JObj ToJObj ();

    public virtual void PostLoad () { }
    /*public static T? ToComponent<T> (JObj jObj) where T : Component {
        return jObj.Data as T;
    }*/

}
