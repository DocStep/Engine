namespace Engine;


public class TransformHandle {
    public TransformHandle (Transform initial) {
        _current = initial;
    }

    private Transform _current = null!;
    [Hide] public Transform Current => _current;

    /// called by GameObject.SetTransform when the underlying instance is swapped
    public void Rebind (Transform newTransform) {
        _current = newTransform;
    }

    /// forward whatever you actually use often, e.g.:
    [Hide] public Transform? Parent { get => _current.Parent; set => _current.Parent = value; }
    [Hide] public List<Transform> Children => _current.Children;

    [Hide] public Vector3 Position { get => _current.Position; set => _current.Position = value; }
    [Hide] public Vector3 LocalPosition { get => _current.LocalPosition; set => _current.LocalPosition = value; }

}
