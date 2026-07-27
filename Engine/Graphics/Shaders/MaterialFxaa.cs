namespace Engine.Graphics.Shaders;

public class MaterialFxaa : Material {
    public MaterialFxaa (Shader shader) : base(shader) { }
    public MaterialFxaa (Material material) : base(material) { }

    public const string InvResolution = "uInvResolution";


    public override void Update () {
        SetVector2(InvResolution, new Vector2(1f/Engine.Window.Size.X, 1f/Engine.Window.Size.Y));
    }

}
