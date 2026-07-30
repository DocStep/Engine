namespace Engine.Graphics;


public class MaterialFxaa : Material {
    public MaterialFxaa (Shader shader) : base(shader) { }

    public const string InvResolution = "uInvResolution";


    public override void ApplyCustom () {
        shader.SetVector2(InvResolution, new Vector2(1f/Engine.Window.Size.X, 1f/Engine.Window.Size.Y));
    }

}
