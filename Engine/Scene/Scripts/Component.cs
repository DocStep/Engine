using Newtonsoft.Json;

namespace Engine;


public abstract class Component /*: ISavable*/ {
    public Component () { }

    //[Hide] public readonly Guid Guid = lib.Guid;
    [Hide] public readonly long Id = lib.Id;

    [Hide] public bool Enabled { get; set; } = true;

    [JsonIgnore, Hide] public GameObject gameObject = null!;
    //[JsonIgnore, Hide] public Guid? ownerGuid = null;
    [Hide][Readonly] public abstract string Name { get; }


    public virtual void SetParent (GameObject gameObject) {
        this.gameObject = gameObject;
    }

    public virtual void OnAdd () { }
    public virtual void OnRemove () { }


    //public void PreSave () { }
    //public virtual void PostLoad () { }

}
