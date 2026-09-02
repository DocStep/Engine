using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class MaterialVignette : Material {
    public MaterialVignette (Shader shader) : base(shader) {
        texture = AssetsEngine.tex_Vignette;
    }

    public float Intensity = 0.5f;
    public float Radius = 0.35f;
    public float Softness = 1f;
    public Vector3 Color = Vector3.Zero;
    public Texture? texture = null;


    public override void ApplyCustom () {
        if (texture is null) return;

        shader.Use();
        shader.SetFloat("uVignetteIntensity", Intensity);
        shader.SetFloat("uVignetteRadius", Radius);
        shader.SetFloat("uVignetteSoftness", Softness);
        shader.SetVector3("uVignetteColor", Color);

        //shader.SetTexture("uInputTexture", texture);
        Renderer.GL.BindVertexArray(PostProcessStack.QuadVAO);
        Renderer.GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
    }

}
