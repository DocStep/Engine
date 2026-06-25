namespace Engine;

public abstract class Component {

    public TransformComponent owner = null!;

    public TransformComponent? parent = null;


}
