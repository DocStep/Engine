using Newtonsoft.Json;

namespace Engine;


public abstract class Component {

    [Hide][Readonly][JsonIgnore] public virtual string Name => GetType().Name;

    [Hide] public bool Enabled { get; set; } = true;

    [Hide, Readonly] public readonly long Id = Lib.Id;

    [JsonIgnore, Hide] public GameObject gameObject = null!;


    public virtual void SetParent (GameObject gameObject) {
        this.gameObject = gameObject;
    }

    public virtual void OnAdd () { }
    public virtual void OnRemove () { }


    //public void PreSave () { }
    //public virtual void PostLoad () { }

}
