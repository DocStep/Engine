namespace Engine;

public abstract class Component {

    public GameObject owner = null!;

    public TransformComponent? parent = null;


    public virtual void SetParent (GameObject gameObject) {
        owner = gameObject;
    }

    public virtual void OnAdd () { }
    public virtual void OnRemove () { }

}
