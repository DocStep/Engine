using Newtonsoft.Json;

namespace Engine;


public class CapsuleColliderComponent : ColliderComponent {

    [JsonIgnore] public override string Name => nameof(CapsuleColliderComponent);

    public Vector3 Position = Vector3.Zero;
    public float Height = 1f;
    public float Radius = 0.5f;


    public override void Update () { }


}
