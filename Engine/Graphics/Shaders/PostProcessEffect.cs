using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class PostProcessEffect {
    public PostProcessEffect (Material material) {
        this.material = material;
    }

    protected readonly Material material;


    public void Apply (uint inputTexture) {
        material.shader.Use();
        Renderer.GL.ActiveTexture(TextureUnit.Texture0);
        Renderer.GL.BindTexture(TextureTarget.Texture2D, inputTexture);
        material.Apply();
        material.shader.SetInt(Shader.Scene, 0);

        Renderer.GL.BindVertexArray(PostProcessStack.QuadVAO);
        Renderer.GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        Renderer.Instance.Stats.DrawCalls++;
        Renderer.GL.BindVertexArray(0);
    }

}
