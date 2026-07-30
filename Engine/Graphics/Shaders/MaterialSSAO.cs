using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class MaterialSSAO : Material {
    public MaterialSSAO (Shader shader) : base(shader) { }

    public Matrix4x4 invProjection;
    public float radius = 0.5f;
    public float bias = 0.01f;
    public float strength = 0.3f;

    public const string InvProjection = "uInvProjection";
    public const string TexelSize = "uTexelSize";
    public const string Radius = "uRadius";
    public const string Bias = "uBias";
    public const string Strength = "uStrength";


    public override void ApplyCustom () {
        Matrix4x4.Invert(Renderer.Instance.m4x4Projection, out invProjection);
        shader.SetMatrix4x4(InvProjection, invProjection);
        shader.SetVector2(TexelSize, new Vector2(1f/Renderer.Instance.Width, 1f/Renderer.Instance.Height));
        shader.SetFloat(Radius, radius);
        shader.SetFloat(Bias, bias);
        shader.SetFloat(Strength, strength);
    }

}
