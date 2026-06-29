namespace Engine.Graphics;


public class MeshComponent : Component, IRenderComponent, IComponentUpdate {

    public Mesh? mesh = null;
    public Shader shader = Renderer.Instance._sh_Lit;
    public Material material = Renderer.Instance._mat_Lit;

    private RenderInfo renderInfo;


    public void Update () {
        if (mesh is null || shader is null || material is null) return;

        Renderer.Instance.AddRenderInfo(CreateRenderInfo);
    }


    public RenderInfo CreateRenderInfo {
        get {
            if (mesh is null) return default;

            RenderInfo renderInfo = new RenderInfo() {
                pos = owner.Transform.Position,
                rot = owner.Transform.Rotation,
                scale = owner.Transform.Scale,

                mesh = mesh,
                shader = shader,
                material = material,
                primitiveType = mesh.Data.PrimitiveType,
            };
            return renderInfo;
        }
    }

}
