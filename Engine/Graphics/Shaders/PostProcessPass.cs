using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class PostProcessPass {
    public PostProcessPass (Material material) {
        this.material = material;
    }

    public bool Enabled = true;
    protected readonly Material material;


    public void Apply (uint inputTexture, uint depthTexture) {
        Renderer.GL.ActiveTexture(TextureUnit.Texture0);
        Renderer.GL.BindTexture(TextureTarget.Texture2D, inputTexture);

        Renderer.GL.ActiveTexture(TextureUnit.Texture1);
        Renderer.GL.BindTexture(TextureTarget.Texture2D, depthTexture);

        material.shader.Use();
        material.Apply();
        material.shader.SetInt(Shader.uScene, 0);
        material.shader.SetInt(Shader.uDepth, 1);

        // Bind scene normal texture only when the shader actually expects it (avoid unit collisions)
        if (material.shader.ActiveUniforms.ContainsKey("uNormal")) {
            Renderer.GL.ActiveTexture(TextureUnit.Texture2);
            Renderer.GL.BindTexture(TextureTarget.Texture2D, Renderer.Instance.PostProcess.SceneNormalTexture);
            material.shader.SetInt("uNormal", 2);
        }

        Renderer.GL.BindVertexArray(PostProcessStack.QuadVAO);
        Renderer.GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        Renderer.GL.BindVertexArray(0);

        Renderer.Instance.Stats.DrawCalls++;
        Renderer.Instance.Stats.PostProccessCalls++;
    }

}
