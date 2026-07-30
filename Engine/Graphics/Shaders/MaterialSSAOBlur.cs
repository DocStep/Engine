using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class MaterialSSAOBlur : Material {
    public MaterialSSAOBlur (Shader shader) : base(shader) { }

    public Matrix4x4 invProjection;
    public float radius = 0.5f;
    public float bias = 0.02f;
    public float strength = 1.0f;

    public const string TexelSize = "uTexelSize";


    public override void ApplyCustom () {
        shader.SetVector2(TexelSize, new Vector2(1f/Renderer.Instance.Width, 1f/Renderer.Instance.Height));
    }

}
