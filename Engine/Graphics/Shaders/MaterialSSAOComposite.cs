using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class MaterialSSAOComposite : Material {
    public MaterialSSAOComposite (Shader shader) : base(shader) { }

    public const string uOriginal = "uOriginal";


    public override void ApplyCustom () {
        Renderer.GL.ActiveTexture(TextureUnit.Texture1);
        Renderer.GL.BindTexture(TextureTarget.Texture2D, Renderer.Instance.PostProcess.TonemappedTexture);
        shader.SetInt(uOriginal, 1);
    }

}
