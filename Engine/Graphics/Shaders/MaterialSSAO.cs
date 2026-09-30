using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class MaterialSSAO : Material {
    public MaterialSSAO (Shader shader) : base(shader) { }

    [Hide] public Matrix4x4 invProjection;
    public float radius = 0.5f;
    public float bias = 0.05f;
    public float strength = 1f;
    //public float power = 1.5f;
    public float sampleCount = 32f;


    public override void ApplyCustom () {
        if (Renderer.Instance.Camera is null) return;

        Matrix4x4.Invert(Renderer.Instance.m4x4_Projection, out invProjection);

        shader.SetMatrix4x4(Shader.uProjection, Renderer.Instance.m4x4_Projection);
        shader.SetMatrix4x4(Shader.uInvProjection, invProjection);
        shader.SetVector2(Shader.uTexelSize, new Vector2(1f/Renderer.Instance.Width, 1f/Renderer.Instance.Height));
        shader.SetFloat(Shader.uRadius, radius);
        shader.SetFloat(Shader.uBias, bias);
        shader.SetFloat(Shader.uStrength, strength);
        shader.SetFloat(Shader.uNear, Renderer.Instance.Camera.PlaneNear);
        shader.SetFloat(Shader.uFar, Renderer.Instance.Camera.PlaneFar);
        shader.SetFloat(Shader.uSampleCount, sampleCount);
        shader.SetInt(Shader.uNormal, 2);
    }

}
