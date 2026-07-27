using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class PostProcessPass {
    public PostProcessPass (Material material) {
        this.material = material;
    }

    protected readonly Material material;


    public void Apply (uint inputTexture, uint depthTexture) {
        Renderer.GL.ActiveTexture(TextureUnit.Texture0);
        Renderer.GL.BindTexture(TextureTarget.Texture2D, inputTexture);

        Renderer.GL.ActiveTexture(TextureUnit.Texture1);
        Renderer.GL.BindTexture(TextureTarget.Texture2D, depthTexture);

        material.shader.Use();
        material.Apply();
        material.shader.SetInt(Shader.Scene, 0);
        material.shader.SetInt(Shader.Depth, 1);

        Renderer.GL.BindVertexArray(PostProcessStack.QuadVAO);
        Renderer.GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        Renderer.GL.BindVertexArray(0);

        Renderer.Instance.Stats.DrawCalls++;
    }

}
