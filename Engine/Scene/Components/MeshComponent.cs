using Newtonsoft.Json;

namespace Engine.Graphics;


public class MeshComponent : Component, IComponentUpdate, IUpdateAtFreeze {

    [JsonIgnore] public override string Name => nameof(MeshComponent);

    [JsonIgnore] public Mesh? mesh = null;
    [Hide][JsonProperty("mesh")] public string? meshPath = null;
    [JsonIgnore] public Material? material = AssetsEngine._mat_Lit;
    [Hide][JsonProperty("material")] public string? materialPath = null;
    //[JsonProperty("pass")] public RenderPass pass = RenderPass.Opaque;

    [Hide][JsonIgnore] public RenderInfo renderInfo { get; private set; }


    public void Update () {
        if (mesh is null || material is null) return;

        //if (mesh?.Name == "SuzanneHighRes") 
            //Log.log($"[{Guid}] AddRenderInfo {mesh?.Name}");
        Renderer.Instance.AddRenderInfo(CreateRenderInfo);
    }

    [Hide][JsonIgnore] public RenderInfo CreateRenderInfo {
        get {
            if (mesh is null) return default;
            if (material is null) return default;

            RenderInfo renderInfo = new RenderInfo() {
                model = gameObject.Transform.GetWorldMatrix(),

                mesh = mesh,
                material = material,
                primitiveType = mesh.Data is not null ? mesh.Data.PrimitiveType : default,
            };

            this.renderInfo = renderInfo;
            return renderInfo;
        }
    }


    /*public override void DrawInspector () {
        ImGuiNET.ImGui.TextDisabled("Mesh: " + mesh?.Name);
        ImGuiNET.ImGui.TextDisabled("Shader: " + material?.shader.Name);
    }*/

}
