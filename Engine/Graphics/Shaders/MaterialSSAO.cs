using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class MaterialSSAO : Material {
    public MaterialSSAO (Shader shader) : base(shader) { }

    public Matrix4x4 invProjection;
    public float radius = 0.3f;
    public float bias = 0.02f;
    public float strength = 0.6f;
    public float falloffPower = 10.0f;

    public const string TexelSize = "uTexelSize";
    public const string Radius = "uRadius";
    public const string Bias = "uBias";
    public const string Strength = "uStrength";
    public const string Near = "uNear";
    public const string Far = "uFar";
    public const string FalloffPower = "uFalloffPower";


    public override void ApplyCustom () {
        Matrix4x4.Invert(Renderer.Instance.m4x4_Projection, out invProjection);

        shader.SetMatrix4x4(Shader.Projection, Renderer.Instance.m4x4_Projection);
        shader.SetMatrix4x4(Shader.InvProjection, invProjection);
        shader.SetVector2(TexelSize, new Vector2(1f/Renderer.Instance.Width, 1f/Renderer.Instance.Height));
        shader.SetFloat(Radius, radius);
        shader.SetFloat(Bias, bias);
        shader.SetFloat(Strength, strength);
        shader.SetFloat(Near, Camera.Current.planeNear);
        shader.SetFloat(Far, Camera.Current.planeFar);
        shader.SetFloat(FalloffPower, falloffPower);
    }

}
