using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class FxaaEffect : PostProcessEffect {

    private readonly Shader _sh;
    private const string InvResolution = "uInvResolution";

    public FxaaEffect (Shader shader) {
        _sh = shader;
    }

    public override void Apply (uint inputTexture) {
        _sh.Use();
        Renderer.GL.ActiveTexture(TextureUnit.Texture0);
        Renderer.GL.BindTexture(TextureTarget.Texture2D, inputTexture);
        _sh.SetInt(Shader.Scene, 0);
        _sh.SetVector2(InvResolution, 1f/Engine.Window.Size.X, 1f/Engine.Window.Size.Y);

        Renderer.GL.BindVertexArray(PostProcessStack.QuadVAO);
        Renderer.GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        Renderer.Instance.Stats.DrawCalls++;
        Renderer.GL.BindVertexArray(0);
    }

}
