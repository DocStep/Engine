using System.Numerics;
using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class ShadowMap : IDisposable {
    public ShadowMap (int size = 2048) {
        GL gl = Renderer.GL;
        Size = size;
        Depth = Texture.CreateDepth(size);

        Fbo = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, Fbo);
        gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, Depth.Handle, 0);
        gl.DrawBuffer(DrawBufferMode.None);
        gl.ReadBuffer(ReadBufferMode.None);

        GLEnum status = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
            Log.log($"ShadowMap FBO incomplete: {status}");

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }


    public Texture Depth = null!;
    public uint Fbo;
    public int Size;
    public Matrix4x4 LightSpace = Matrix4x4.Identity;
    public float TexelWorld;


    /// <summary> Fits an ortho box around a sphere (center, radius), usually around the camera. </summary>
    public void Begin (Vector3 lightDir, Vector3 center, float radius) {
        GL gl = Renderer.GL;

        float pad = 100f; /// extra distance toward the sun for tall casters

        float texel = radius*2f/Size;
        Vector3 dir = Vector3.Normalize(lightDir);
        Vector3 up = MathF.Abs(dir.Y) > 0.99f ? Vector3.UnitZ : Vector3.UnitY;

        /// Fixed light basis at the world origin, independent of the camera
        Matrix4x4 basis = Matrix4x4.CreateLookAtLeftHanded(Vector3.Zero, dir, up);
        Vector3 c = Vector3.Transform(center, basis);
        c.X = MathF.Floor(c.X/texel)*texel;
        c.Y = MathF.Floor(c.Y/texel)*texel;
        Matrix4x4.Invert(basis, out Matrix4x4 inv);
        center = Vector3.Transform(c, inv);

        Matrix4x4 view = Matrix4x4.CreateLookAtLeftHanded(center - dir*(radius + pad), center, up);
        Matrix4x4 proj = Matrix4x4.CreateOrthographicLeftHanded(radius*2f, radius*2f, 0.1f, radius*2f + pad);
        LightSpace = view*proj;
        LightSpace = view*proj;
        gl.Enable(EnableCap.DepthClamp);
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, Fbo);
        gl.Viewport(0, 0, (uint)Size, (uint)Size);
        gl.Disable(EnableCap.Blend);
        gl.Enable(EnableCap.DepthTest);
        gl.DepthMask(true);
        gl.ColorMask(false, false, false, false);
        gl.Clear((uint)ClearBufferMask.DepthBufferBit);

        /// Slope-scaled bias here replaces most of the bias in the fragment shader
        //TexelWorld = radius*2f/Size;
        //gl.Enable(EnableCap.PolygonOffsetFill);
        //gl.PolygonOffset(1f, 2f);
    }

    public void End () {
        GL gl = Renderer.GL;
        gl.Disable(EnableCap.DepthClamp);
        gl.Disable(EnableCap.PolygonOffsetFill);
        gl.ColorMask(true, true, true, true);
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Dispose () {
        Renderer.GL.DeleteFramebuffer(Fbo);
        Depth.Dispose();
    }

}