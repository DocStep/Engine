using System.Reflection;
using Engine.Graphics;
using Newtonsoft.Json;

namespace Engine;


public abstract class Component : ISavable {
    public Component () { }

    [Hide] public readonly Guid Guid = lib.Guid;
    [Hide] public readonly long Id = lib.Id;

    [Hide] public bool Enabled { get; set; } = true;

    [Hide][JsonIgnore] public GameObject owner = null!;
    [Hide] public Guid? ownerGuid = null; /// For save
    [Hide][JsonIgnore] public Transform? parent = null;
    [Hide] public Guid? parentGuid = null; /// For save
    [Hide][Readonly] public abstract string Name { get; }


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
