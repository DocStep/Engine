using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class GrayscaleEffect : PostProcessEffect {

    Shader _sh;
    public GrayscaleEffect (Shader shader) { _sh = shader; }


    public override void Apply (uint inputTexture) {
        _sh.Use();
        Renderer.GL.ActiveTexture(TextureUnit.Texture0);
        Renderer.GL.BindTexture(TextureTarget.Texture2D, inputTexture);
        _sh.SetInt("uScene", 0);

        Renderer.GL.BindVertexArray(PostProcessStack.QuadVAO);
        Renderer.GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        Renderer.Instance.Stats.DrawCalls++;
        Renderer.GL.BindVertexArray(0);
    }

}
