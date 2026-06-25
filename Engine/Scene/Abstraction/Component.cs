namespace Engine;

public abstract class Component {

    public GameObject owner = null!;

    public TransformComponent? parent = null;


}
