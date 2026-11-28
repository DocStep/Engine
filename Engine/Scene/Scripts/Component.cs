using Newtonsoft.Json;

namespace Engine;


public abstract class Component {

    [Hide][Readonly][JsonIgnore] public virtual string Name => GetType().Name;

    [JsonProperty("Enabled"), Hide] private bool enabled = true;
    [Hide][JsonIgnore]
    public bool Enabled {
        get => enabled && (_gameObject is null || _gameObject.IsActiveInHierarchy);
        set {
            bool wasEnabled = Enabled;
            enabled = value;
            NotifyEnabledChanged(wasEnabled, Enabled);
            Scene.MarkDirtyIfEditing();
        }
    }

    [Hide, Readonly] public readonly long Id = Lib.Id;

    [Hide, JsonIgnore] protected GameObject _gameObject = null!;
    [Hide] public GameObject gameObject {
        get => _gameObject;
        set => _gameObject = value;
    }

    [Hide] public Transform transform {
        get {
            return _gameObject?.Transform!;
        }
    }


    public virtual void OnAdd () { }
    public virtual void OnRemove () { }
    protected virtual void OnEnabledChanged (bool enabled) { }

    internal void NotifyEnabledChanged (bool wasActive, bool isActive) {
        bool wasEnabled = enabled && wasActive;
        bool isEnabled = enabled && isActive;
        if (wasEnabled != isEnabled) OnEnabledChanged(isEnabled);
    }

}
