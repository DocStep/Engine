using Newtonsoft.Json;

namespace Engine;


public class PlaneColliderComponent : ColliderComponent {

    [JsonIgnore] public override string Name => nameof(PlaneColliderComponent);

    public Vector3 Position = Vector3.Zero;
    public Vector3 Rotation = Vector3.Zero;
    public Vector3 Scale = Vector3.One;


    public override void Update () { }

}
