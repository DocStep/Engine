using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class MaterialCameraFocus : Material {
    public MaterialCameraFocus (Shader shader) : base(shader) { }

    public float focusDistance = 10f;
    public float focusRange = 5f;
    public float bokehRadius = 3.0f;

    public const string SceneDepth = "uDepth";
    public const string FocusDistance = "uFocusDistance";
    public const string FocusRange = "uFocusRange";
    public const string BokehRadius = "uBokehRadius";


    public override void ApplyCustom () {
        if (Renderer.Instance.Camera is null) return;

        //shader.SetInt(SceneDepth, 1);
        shader.SetFloat(Shader.uNear, Renderer.Instance.Camera.Near);
        shader.SetFloat(Shader.uFar, Renderer.Instance.Camera.Far);
        //shader.SetFloat(FocusDistance, focusDistance);
        //shader.SetFloat(FocusRange, focusRange);
        //shader.SetFloat(BokehRadius, bokehRadius);
        shader.SetVector2(Shader.uTexelSize, new Vector2(1f/Renderer.Instance.Width, 1f/Renderer.Instance.Height));
    }

}
