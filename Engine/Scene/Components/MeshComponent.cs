using Newtonsoft.Json;

namespace Engine.Graphics;


public class MeshComponent : Component, IComponentUpdate, IUpdateAtFreeze {

    [JsonIgnore] public override string Name => nameof(MeshComponent);

    [JsonIgnore] public Mesh? mesh = null;
    [JsonProperty("mesh")] public string? meshPath = null;
    //[JsonIgnore] public Shader shader = AssetsEngine._sh_Lit;
    //[JsonProperty("shader")] public string? shaderPath = null;
    [JsonIgnore] public Material? material = AssetsEngine._mat_Lit;
    [JsonProperty("material")] public string? materialPath = null;
    //[JsonProperty("pass")] public RenderPass pass = RenderPass.Opaque;

    [JsonIgnore] private RenderInfo renderInfo;


    public void Update () {
        if (mesh is null || material is null) return;

        Renderer.Instance.AddRenderInfo(CreateRenderInfo);
    }

    [JsonIgnore] public RenderInfo CreateRenderInfo {
        get {
            if (mesh is null) return default;
            if (material is null) return default;

            RenderInfo renderInfo = new RenderInfo() {
                pos = owner.Transform.Position,
                rot = owner.Transform.Rotation,
                scale = owner.Transform.Scale,

                mesh = mesh,
                material = material,
                primitiveType = mesh.Data is not null ? mesh.Data.PrimitiveType : default,
            };
            return renderInfo;
        }
    }


    /*public override void DrawInspector () {
        ImGuiNET.ImGui.TextDisabled("Mesh: " + mesh?.Name);
        ImGuiNET.ImGui.TextDisabled("Shader: " + material?.shader.Name);
    }*/

}
