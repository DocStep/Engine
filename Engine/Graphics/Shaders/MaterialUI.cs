namespace Engine.Graphics.UI;


public class MaterialUI : Material {
    public MaterialUI (Shader shader) : base(shader) {
        pass = RenderPass.UI;
        face = RenderFace.Both;
        depthTest = false;
        depthWrite = false;
    }

    public uint textureId;


    public override void ApplyCustom () {
        Renderer.GL.ActiveTexture(Silk.NET.OpenGL.TextureUnit.Texture0);
        Renderer.GL.BindTexture(Silk.NET.OpenGL.TextureTarget.Texture2D, textureId);
    }

}
