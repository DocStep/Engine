using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class MaterialSSAO : Material {
    public MaterialSSAO (Shader shader) : base(shader) { }

    [Hide] public Matrix4x4 invProjection;
    public float radius = 0.3f;
    public float bias = 0.02f;
    public float strength = 0.6f;
    //public float falloffPower = 10.0f;

    [Hide] public const string TexelSize = "uTexelSize";
    [Hide] public const string Radius = "uRadius";
    [Hide] public const string Bias = "uBias";
    [Hide] public const string Strength = "uStrength";
    [Hide] public const string Near = "uNear";
    [Hide] public const string Far = "uFar";
    //public const string FalloffPower = "uFalloffPower";


    public override void ApplyCustom () {
        if (Renderer.Instance.Camera is null) return;

        Matrix4x4.Invert(Renderer.Instance.m4x4_Projection, out invProjection);

        shader.SetMatrix4x4(Shader.Projection, Renderer.Instance.m4x4_Projection);
        shader.SetMatrix4x4(Shader.InvProjection, invProjection);
        shader.SetVector2(TexelSize, new Vector2(1f/Renderer.Instance.Width, 1f/Renderer.Instance.Height));
        shader.SetFloat(Radius, radius);
        shader.SetFloat(Bias, bias);
        shader.SetFloat(Strength, strength);
        shader.SetFloat(Near, Renderer.Instance.Camera.PlaneNear);
        shader.SetFloat(Far, Renderer.Instance.Camera.PlaneFar);
        //shader.SetFloat(FalloffPower, falloffPower);

        // Bind the scene normals (stored as view-space floats in the postprocess stack) to the shader.
        // Use texture unit 2 to avoid clashing with Scene (0) and Depth (1).
        Renderer.GL.ActiveTexture(Silk.NET.OpenGL.TextureUnit.Texture2);
        Renderer.GL.BindTexture(Silk.NET.OpenGL.TextureTarget.Texture2D, Renderer.Instance.PostProcess.SceneNormalTexture);
        shader.SetInt("uNormal", 2);
        //Log.log("SceneNormalTexture", Renderer.Instance.PostProcess.SceneNormalTexture);
    }

}
