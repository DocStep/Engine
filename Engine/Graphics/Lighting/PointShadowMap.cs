using System.Numerics;
using Silk.NET.OpenGL;

namespace Engine.Graphics;


public unsafe class PointShadowMap : IDisposable {

    public const int MaxLights = 4;
    public const int Size = 512;

    [Hide] public uint Depth;
    [Hide] public uint Fbo;
    [Hide] public readonly PointLight?[] Slots = new PointLight?[MaxLights];

    static readonly Vector3[] Dirs = { Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ };
    static readonly Vector3[] Ups = { -Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ, -Vector3.UnitY, -Vector3.UnitY };


    public PointShadowMap () {
        GL gl = Renderer.GL;

        Depth = gl.GenTexture();
        gl.BindTexture(TextureTarget.TextureCubeMapArray, Depth);
        gl.TexImage3D(TextureTarget.TextureCubeMapArray, 0, InternalFormat.DepthComponent24,
            (uint)Size, (uint)Size, (uint)(6*MaxLights), 0, PixelFormat.DepthComponent, PixelType.Float, null);
        gl.TexParameter(TextureTarget.TextureCubeMapArray, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        gl.TexParameter(TextureTarget.TextureCubeMapArray, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        gl.TexParameter(TextureTarget.TextureCubeMapArray, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMapArray, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMapArray, TextureParameterName.TextureWrapR, (int)GLEnum.ClampToEdge);

        Fbo = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, Fbo);
        gl.FramebufferTextureLayer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, Depth, 0, 0);
        gl.DrawBuffer(DrawBufferMode.None);
        gl.ReadBuffer(ReadBufferMode.None);

        GLEnum status = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
            Log.log($"PointShadowMap FBO incomplete: {status}");

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    /// RH matrices on purpose: cubemap face lookup is defined on world directions, so this matches
    /// the standard face table no matter what handedness the main camera uses.
    public static Matrix4x4 FaceMatrix (Vector3 pos, float range, int face) {
        Matrix4x4 view = Matrix4x4.CreateLookAt(pos, pos + Dirs[face], Ups[face]);
        Matrix4x4 proj = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI*0.5f, 1f, 0.05f, range);
        return view*proj;
    }

    public void Begin () {
        GL gl = Renderer.GL;
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, Fbo);
        gl.Viewport(0, 0, (uint)Size, (uint)Size);
        gl.Disable(EnableCap.ScissorTest);
        gl.Disable(EnableCap.Blend);
        gl.Disable(EnableCap.PolygonOffsetFill); /// ignored when the shader writes gl_FragDepth
        //gl.PolygonOffset(1.5f, 3f);
        gl.Enable(EnableCap.DepthTest);
        gl.DepthFunc(DepthFunction.Less);
        gl.DepthMask(true);
        gl.ColorMask(false, false, false, false);
    }

    public void BeginFace (int slot, int face) {
        Renderer.GL.FramebufferTextureLayer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, Depth, 0, slot*6 + face);
        Renderer.GL.Clear((uint)ClearBufferMask.DepthBufferBit);
    }

    public void End () {
        Renderer.GL.ColorMask(true, true, true, true);
        Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        Renderer.GL.Disable(EnableCap.PolygonOffsetFill);
    }

    public void Bind (TextureUnit unit) {
        Renderer.GL.ActiveTexture(unit);
        Renderer.GL.BindTexture(TextureTarget.TextureCubeMapArray, Depth);
    }

    public void Dispose () {
        Renderer.GL.DeleteFramebuffer(Fbo);
        Renderer.GL.DeleteTexture(Depth);
    }

}
