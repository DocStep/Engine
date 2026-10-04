using Newtonsoft.Json;

namespace Engine;


public class PlaneColliderComponent : MeshColliderComponent {

    [JsonIgnore] public override string Name => nameof(PlaneColliderComponent);

    public override void OnAdd () {
        Mesh = AssetsEngine._mesh_PlaneQuad;
        base.OnAdd();
    }

}
