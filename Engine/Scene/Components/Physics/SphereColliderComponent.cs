using Newtonsoft.Json;

namespace Engine;


public class SphereColliderComponent : ColliderComponent {

    [JsonIgnore] public override string Name => nameof(SphereColliderComponent);

    public Vector3 Position = Vector3.Zero;
    public float Radius = 0.5f;


    public override void Update () { }

}
