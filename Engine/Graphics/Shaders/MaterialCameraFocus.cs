using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class MaterialCameraFocus : Material {
    public MaterialCameraFocus (Shader shader) : base(shader) { }

    public float focusDistance = 10f;
    public float focusRange = 5f;
    public float bokehRadius = 0.0f;

    public const string SceneDepth = "uDepth";
    public const string Near = "uNear";
    public const string Far = "uFar";
    public const string FocusDistance = "uFocusDistance";
    public const string FocusRange = "uFocusRange";
    public const string BokehRadius = "uBokehRadius";
    public const string TexelSize = "uTexelSize";


    public override void ApplyCustom () {
        shader.SetInt(SceneDepth, 1);
        shader.SetFloat(Near, Camera.planeNear);
        shader.SetFloat(Far, Camera.planeFar);
        shader.SetFloat(FocusDistance, focusDistance);
        shader.SetFloat(FocusRange, focusRange);
        shader.SetFloat(BokehRadius, bokehRadius);
        shader.SetVector2(TexelSize, new Vector2(1f/Renderer.Instance.Width, 1f/Renderer.Instance.Height));
    }

}
