using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class PostProcessPass {
    public PostProcessPass (Material material) {
        Material = material;
    }

    public bool Enabled = true;

    public bool Ldr = false; /// true = runs after tonemap
    [Newtonsoft.Json.JsonProperty] protected readonly Material Material;
    public Action<Shader>? OnApply = null; /// per-frame uniforms, called after the shader is bound


    public void Apply (uint inputTexture, uint depthTexture) {
        Renderer.GL.ActiveTexture(TextureUnit.Texture0);
        Renderer.GL.BindTexture(TextureTarget.Texture2D, inputTexture);

        Renderer.GL.ActiveTexture(TextureUnit.Texture1);
        Renderer.GL.BindTexture(TextureTarget.Texture2D, depthTexture);

        Material.shader.Use();
        Material.Apply();
        Material.shader.SetInt(Shader.uScene, 0);
        Material.shader.SetInt(Shader.uDepth, 1);

        // Bind scene normal texture only when the shader actually expects it (avoid unit collisions)
        if (Material.shader.ActiveUniforms.ContainsKey(Shader.uNormal)) {
            Renderer.GL.ActiveTexture(TextureUnit.Texture2);
            Renderer.GL.BindTexture(TextureTarget.Texture2D, Renderer.Instance.PostProcess.SceneNormalTexture);
            Material.shader.SetInt(Shader.uNormal, 2);
        }

        OnApply?.Invoke(Material.shader);

        Renderer.GL.BindVertexArray(PostProcessStack.QuadVAO);
        Renderer.GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        Renderer.GL.BindVertexArray(0);

        Renderer.Instance.Stats.DrawCalls++;
        Renderer.Instance.Stats.PostProccessCalls++;
    }

}
