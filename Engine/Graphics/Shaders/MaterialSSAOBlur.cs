using System.Numerics;
using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class MaterialSSAOBlur : Material {
    public MaterialSSAOBlur (Shader shader) : base(shader) { }

    /// relative to depth: lower = crisper edges, higher = smoother on slanted surfaces
    public float depthThreshold = 0.05f;

    public const string DepthThreshold = "uDepthThreshold";


    public override void ApplyCustom () {
        if (Renderer.Instance is null || Renderer.Instance.Camera is null) return;

        shader.SetVector2(Shader.uTexelSize, new Vector2(1f/Renderer.Instance.Width, 1f/Renderer.Instance.Height));
        shader.SetFloat(Shader.uNear, Renderer.Instance.Camera.Near);
        shader.SetFloat(Shader.uFar, Renderer.Instance.Camera.Far);
        shader.SetFloat(DepthThreshold, depthThreshold);
    }

}