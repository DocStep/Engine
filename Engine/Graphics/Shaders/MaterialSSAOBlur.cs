using System.Numerics;
using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class MaterialSSAOBlur : Material {
    public MaterialSSAOBlur (Shader shader) : base(shader) { }

    public const string TexelSize = "uTexelSize";
    public const string Near = "uNear";
    public const string Far = "uFar";
    public const string DepthThreshold = "uDepthThreshold";

    /// relative to depth: lower = crisper edges, higher = smoother on slanted surfaces
    public float depthThreshold = 0.05f;


    public override void ApplyCustom () {
        if (Renderer.Instance is null || Renderer.Instance.Camera is null) return;

        shader.SetVector2(TexelSize, new Vector2(1f/Renderer.Instance.Width, 1f/Renderer.Instance.Height));
        shader.SetFloat(Near, Renderer.Instance.Camera.PlaneNear);
        shader.SetFloat(Far, Renderer.Instance.Camera.PlaneFar);
        shader.SetFloat(DepthThreshold, depthThreshold);
    }

}