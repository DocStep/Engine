namespace Engine.Graphics;

public class MaterialSkybox : Material {
    public MaterialSkybox (Shader shader) : base(shader) { }

    public float blurScale = 0f;

    public const string BlurScale = "uBlurScale";


    public override void ApplyCustom () {
        Matrix4x4 view = Renderer.Instance.m4x4_View;
        Matrix4x4 projection = Renderer.Instance.m4x4_Projection;
        Matrix4x4.Invert(view, out view);
        Matrix4x4.Invert(projection, out projection);

        shader.Use();
        shader.SetMatrix4x4(Shader.View, view);
        shader.SetMatrix4x4(Shader.Projection, projection);
        shader.SetInt(Shader.Texture, 0);
        shader.SetFloat(BlurScale, blurScale);
    }

}
