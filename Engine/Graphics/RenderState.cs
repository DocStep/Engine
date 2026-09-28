using Silk.NET.OpenGL;

namespace Engine.Graphics;


/// Remembers the last bound material/shader so redundant GL state changes are skipped.
public sealed class RenderState {

    private Material? _material = null;
    private Shader? _shader = null;

    private static GL GL => Renderer.GL;


    /// Applies material GL state and shader only if they changed, returns the shader
    public Shader Bind (Material material) {
        if (!ReferenceEquals(material, _material)) ApplyMaterialState(material);

        Shader shader = material.shader;
        if (!ReferenceEquals(shader, _shader)) shader.Use();

        _material = material;
        _shader = shader;
        return shader;
    }

    private static void ApplyMaterialState (Material material) {
        /// Pass
        switch (material.Pass) {
            case RenderPass.Opaque:
                GL.Disable(EnableCap.Blend);
                break;
            case RenderPass.Transparent:
            case RenderPass.UI:
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                break;
        }

        /// CullFace
        switch (material.Face) {
            case RenderFace.Front:
                GL.Enable(EnableCap.CullFace);
                GL.CullFace(TriangleFace.Back);
                break;
            case RenderFace.Back:
                GL.Enable(EnableCap.CullFace);
                GL.CullFace(TriangleFace.Front);
                break;
            case RenderFace.Both:
                GL.Disable(EnableCap.CullFace);
                break;
        }

        /// Depth
        if (material.DepthTest) GL.Enable(EnableCap.DepthTest);
        else GL.Disable(EnableCap.DepthTest);
        GL.DepthMask(material.DepthWrite);
    }

    /// Call at frame start — GL state from post-process/gizmos/editor UI may not match
    /// the first material we draw, so force the next Bind to re-apply everything
    public void Reset () {
        _material = null;
        _shader = null;
    }

}
