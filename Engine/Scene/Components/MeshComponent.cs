using Newtonsoft.Json;

namespace Engine.Graphics;


public class MeshComponent : Component, IRenderComponent, IComponentUpdate {

    [JsonIgnore] public readonly static string typeName = typeof(TransformComponent).Name;

    [JsonIgnore] public Mesh? mesh = null;
    [JsonProperty("mesh")] public string? meshPath = null;
    [JsonIgnore] public Shader shader = Renderer.Instance._sh_Lit;
    [JsonProperty("shader")] public string? shaderPath = null;
    [JsonIgnore] public Material material = Renderer.Instance._mat_Lit;
    [JsonProperty("material")] public string? materialPath = null;

    [JsonIgnore] private RenderInfo renderInfo;


    public void Update () {
        if (mesh is null || shader is null || material is null) return;

        Renderer.Instance.AddRenderInfo(CreateRenderInfo);
    }


    [JsonIgnore] public RenderInfo CreateRenderInfo {
        get {
            if (mesh is null) return default;

            RenderInfo renderInfo = new RenderInfo() {
                pos = owner.Transform.Position,
                rot = owner.Transform.Rotation,
                scale = owner.Transform.Scale,

                mesh = mesh,
                shader = shader,
                material = material,
                primitiveType = mesh?.Data is not null ? mesh.Data.PrimitiveType : default,
            };
            return renderInfo;
        }
    }

}
