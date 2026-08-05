using System.Reflection;
using Engine.Graphics;
using Newtonsoft.Json;

namespace Engine;


public abstract class Component : ISavable {
    public Component () { }

    [Hide] public readonly Guid? Guid = lib.Guid;
    [Hide] public readonly long? Id = lib.Id;

    //[JsonIgnore] public bool enabled = true;
    public bool Enabled { get; set; } = true;

    [JsonIgnore] public GameObject owner = null!;
    public Guid? ownerGuid = null; /// For save
    [JsonIgnore] public Transform? parent = null;
    public Guid? parentGuid = null; /// For save
    [Readonly] public abstract string Name { get; }


    public virtual void SetParent (GameObject gameObject) {
        owner = gameObject;
    }

    public virtual void OnAdd () { }
    public virtual void OnRemove () { }



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
