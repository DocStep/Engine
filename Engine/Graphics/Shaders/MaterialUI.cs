namespace Engine.Graphics.UI;


public class MaterialUI : Material {
    public MaterialUI (Shader shader) : base(shader) {
        Pass = RenderPass.UI;
        Face = RenderFace.Both;
        DepthTest = false;
        DepthWrite = false;
    }

    [Hide][Newtonsoft.Json.JsonIgnore] public uint textureId;


    public override void ApplyCustom () {
        Renderer.GL.ActiveTexture(Silk.NET.OpenGL.TextureUnit.Texture0);
        Renderer.GL.BindTexture(Silk.NET.OpenGL.TextureTarget.Texture2D, textureId);
    }

}
