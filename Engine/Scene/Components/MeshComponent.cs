using Newtonsoft.Json;

namespace Engine.Graphics;


public class MeshComponent : Component, IUpdate, IUpdateAtFreeze {

    [JsonIgnore] public override string Name => nameof(MeshComponent);

    public Mesh? Mesh = null;
    /*[JsonIgnore]*/ public Material? Material = AssetsEngine._mat_Lit;
    //[JsonProperty("pass")] public RenderPass pass = RenderPass.Opaque;

    [Hide][JsonIgnore] public RenderInfo renderInfo { get; private set; }


    public void Update () {
        if (Mesh is null || Material is null) return;

        //if (mesh?.Name == "SuzanneHighRes") 
            //Log.log($"[{Guid}] AddRenderInfo {mesh?.Name}");
        Renderer.Instance.AddRenderInfo(CreateRenderInfo);
    }

    [Hide][JsonIgnore] public RenderInfo CreateRenderInfo {
        get {
            if (Mesh is null) return default;
            if (Material is null) return default;

            RenderInfo renderInfo = new RenderInfo() {
                model = gameObject.Transform.GetWorldMatrix(),

                mesh = Mesh,
                material = Material,
                primitiveType = Mesh.Data is not null ? Mesh.Data.PrimitiveType : default,
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
