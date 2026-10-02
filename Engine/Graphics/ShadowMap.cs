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

        Vector3 dir = Vector3.Normalize(lightDir);
        Vector3 eye = center - dir*radius;
        Matrix4x4 view = Matrix4x4.CreateLookAtLeftHanded(eye, center, Vector3.UnitY);
        Matrix4x4 proj = Matrix4x4.CreateOrthographicLeftHanded(radius*2f, radius*2f, 0.1f, Size/1024*radius*2f);
        LightSpace = view*proj;

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, Fbo);
        gl.Viewport(0, 0, (uint)Size, (uint)Size);
        gl.Disable(EnableCap.Blend);
        gl.Enable(EnableCap.DepthTest);
        gl.DepthMask(true);
        gl.ColorMask(false, false, false, false);
        gl.Clear((uint)ClearBufferMask.DepthBufferBit);

        /// Slope-scaled bias here replaces most of the bias in the fragment shader
        gl.Enable(EnableCap.PolygonOffsetFill);
        TexelWorld = radius*2f/Size;
        gl.PolygonOffset(1f, 2f);
    }

    public void End () {
        GL gl = Renderer.GL;
        gl.Disable(EnableCap.PolygonOffsetFill);
        gl.ColorMask(true, true, true, true);
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Dispose () {
        Renderer.GL.DeleteFramebuffer(Fbo);
        Depth.Dispose();
    }

}