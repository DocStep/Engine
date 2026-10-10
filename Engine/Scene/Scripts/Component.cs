using Newtonsoft.Json;

namespace Engine;


public abstract class Component {

    [Hide][Readonly][JsonIgnore] public virtual string Name => GetType().Name;

    [Hide] public bool Enabled { get; set; } = true;

    [Hide, Readonly] public readonly long Id = Lib.Id;

    [Hide, JsonIgnore] protected GameObject _gameObject = null!;
    [Hide] public GameObject gameObject {
        get => _gameObject;
        set => _gameObject = value;
    }



    public virtual void OnAdd () { }
    public virtual void OnRemove () { }

}
