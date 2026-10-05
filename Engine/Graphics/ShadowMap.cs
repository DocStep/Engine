using System.Numerics;
using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class ShadowMap : IDisposable {

    public ShadowMap (int size = 2048) {
        GL gl = Renderer.GL;
        Size = size;

        Depth = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, Depth);
        unsafe {
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent24, (uint)size, (uint)size, 0,
                PixelFormat.DepthComponent, PixelType.Float, null);
        }
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)GLEnum.CompareRefToTexture);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, (int)GLEnum.Lequal);

        Fbo = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, Fbo);
        gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, Depth, 0);
        gl.DrawBuffer(DrawBufferMode.None);
        gl.ReadBuffer(ReadBufferMode.None);
        if (gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
            Log.log("ShadowMap FBO incomplete");
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    public uint Depth, Fbo;
    public int Size;
    public Matrix4x4 LightSpace = Matrix4x4.Identity;
    public float TexelWorld, Bias;

    public void Bind (TextureUnit unit) {
        Renderer.GL.ActiveTexture(unit);
        Renderer.GL.BindTexture(TextureTarget.Texture2D, Depth);
    }

    public void SetSun (Vector3 lightDir, Vector3 center, float radius, float pad, float worldBias) {
        Vector3 d = Vector3.Normalize(lightDir);
        Vector3 up = 0.99f < MathF.Abs(d.Y) ? Vector3.UnitZ : Vector3.UnitY;

        float texel = radius*2f/Size;
        Matrix4x4 basis = Matrix4x4.CreateLookAtLeftHanded(Vector3.Zero, d, up);
        Vector3 c = Vector3.Transform(center, basis);
        c.X = MathF.Floor(c.X/texel)*texel;
        c.Y = MathF.Floor(c.Y/texel)*texel;
        Matrix4x4.Invert(basis, out Matrix4x4 inv);
        center = Vector3.Transform(c, inv);

        float near = 0.1f, far = radius*2f + pad;
        Matrix4x4 view = Matrix4x4.CreateLookAtLeftHanded(center - d*(radius + pad), center, up);
        Matrix4x4 proj = Matrix4x4.CreateOrthographicLeftHanded(radius*2f, radius*2f, near, far);

        LightSpace = view*proj;
        TexelWorld = texel;
        Bias = worldBias*0.5f/(far - near); /// metres -> stored depth (z is mapped to 0.5..1)
    }

    public void Begin () {
        GL gl = Renderer.GL;
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, Fbo);
        gl.Viewport(0, 0, (uint)Size, (uint)Size);
        gl.Disable(EnableCap.ScissorTest);
        gl.Disable(EnableCap.Blend);
        gl.Enable(EnableCap.DepthTest);
        gl.DepthMask(true);
        gl.ColorMask(false, false, false, false);
        gl.Enable(EnableCap.DepthClamp);
        gl.Clear((uint)ClearBufferMask.DepthBufferBit);
        gl.Enable(EnableCap.PolygonOffsetFill);
        gl.PolygonOffset(2f, 4f);
    }

    public void End () {
        GL gl = Renderer.GL;
        gl.Disable(EnableCap.DepthClamp);
        gl.ColorMask(true, true, true, true);
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        gl.Disable(EnableCap.PolygonOffsetFill);
    }

    public void Dispose () {
        Renderer.GL.DeleteFramebuffer(Fbo);
        Renderer.GL.DeleteTexture(Depth);
    }
}
