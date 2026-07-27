using Silk.NET.OpenGL;
using static Engine.Graphics.Shader;
using static Engine.AssetsEngine;

namespace Engine.Graphics;


public class PostProcessStack : IDisposable {
    public PostProcessStack () {
        QuadVAO = Renderer.GL.GenVertexArray();

        Resize(Engine.Window.Size.X, Engine.Window.Size.Y);
    }

    public static uint QuadVAO;

    public List<PostProcessPass> Effects = new();

    /// Final Result
    uint _sceneFbo, _sceneColor, _sceneDepth;
    uint[] _pingFbo = new uint[2];
    uint[] _pingColor = new uint[2];

    uint _outputFbo, _outputColor; /// Final Result
    public uint OutputTexture => _outputColor;
    int _width, _height;


    public void Resize (int w, int h) {
        if (w <= 0 || h <= 0) return;
        if (_width == w && _height == h && _sceneFbo != 0) return;

        DeleteTargets();

        _width = w;
        _height = h;

        _sceneFbo = CreateFbo(w, h, out _sceneColor, out _sceneDepth, withDepth: true);
        _pingFbo[0] = CreateFbo(w, h, out _pingColor[0], out _, withDepth: false);
        _pingFbo[1] = CreateFbo(w, h, out _pingColor[1], out _, withDepth: false);

        _outputFbo = CreateFbo(w, h, out _outputColor, out _, withDepth: false);
    }

    uint CreateFbo (int w, int h, out uint colorTex, out uint depthTex, bool withDepth) {
        uint fbo = Renderer.GL.GenFramebuffer();
        Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
        SetDrawBuffer(fbo);

        colorTex = Renderer.GL.GenTexture();
        Renderer.GL.BindTexture(TextureTarget.Texture2D, colorTex);
        unsafe {
            Renderer.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8,
                (uint)w, (uint)h, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
        }
        Renderer.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        Renderer.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        Renderer.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        Renderer.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        Renderer.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, colorTex, 0);

        depthTex = 0;
        if (withDepth) {
            depthTex = Renderer.GL.GenTexture();
            Renderer.GL.BindTexture(TextureTarget.Texture2D, depthTex);
            unsafe {
                Renderer.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Depth24Stencil8,
                    (uint)w, (uint)h, 0, PixelFormat.DepthStencil, PixelType.UnsignedInt248, null);
            }
            Renderer.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
            Renderer.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
            Renderer.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            Renderer.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            Renderer.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment,
                TextureTarget.Texture2D, depthTex, 0);
        }

        var status = Renderer.GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
            Log.log($"PostProcess FBO incomplete: {status}");

        Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        return fbo;
    }
    public void EndSceneAndRunStack () {
        EndSceneAndRunStack(_outputFbo);
    }


    /// Call before drawing the scene
    public void BeginScene () {
        Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, _sceneFbo);
        SetDrawBuffer(_sceneFbo);
        Renderer.GL.Viewport(0, 0, (uint)_width, (uint)_height);
        Renderer.GL.ColorMask(true, true, true, true);
        Renderer.GL.DepthMask(true);
        Renderer.GL.DepthFunc(DepthFunction.Less);
        Renderer.GL.Disable(EnableCap.StencilTest);
        Renderer.GL.Disable(EnableCap.Blend);
        Renderer.GL.Enable(EnableCap.DepthTest);
        Renderer.GL.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
    }

    /// Call after all scene draws (skybox, meshes, etc). Runs the whole stack, ends drawing to screen (fbo 0).
    public void EndSceneAndRunStack (uint finalTargetFbo) {
        EndSceneAndRunStack(finalTargetFbo, enabled: true);
    }

    public void EndSceneAndRunStack (uint finalTargetFbo, bool enabled) {
        Renderer.GL.Disable(EnableCap.DepthTest);

        if (!enabled || Effects.Count == 0) {
            CopySceneColor(finalTargetFbo);
            CopySceneDepth(finalTargetFbo);
            Renderer.GL.DepthMask(true);
            return;
        }

        uint currentInput = _sceneColor;
        int pingIndex = 0;

        for (int i = 0; i < Effects.Count; i++) {
            bool isLast = i == Effects.Count - 1;
            uint targetFbo = isLast ? finalTargetFbo : _pingFbo[pingIndex];
            Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, targetFbo);
            SetDrawBuffer(targetFbo);
            PrepareFullscreenPass();

            Effects[i].Apply(currentInput, _sceneDepth);

            if (!isLast) {
                currentInput = _pingColor[pingIndex];
                pingIndex = 1 - pingIndex;
            }
        }

        CopySceneDepth(finalTargetFbo);
        Renderer.GL.DepthMask(true);
    }

    /*void Blit (uint tex) {
        PrepareFullscreenPass();

        _sh_Passthrough.Use();
        Renderer.GL.ActiveTexture(TextureUnit.Texture0);
        Renderer.GL.BindTexture(TextureTarget.Texture2D, tex);
        _sh_Passthrough.SetInt(Shader.Scene, 0);

        Renderer.GL.BindVertexArray(QuadVAO);
        Renderer.GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        Renderer.Instance.Stats.DrawCalls++;
        Renderer.GL.BindVertexArray(0);
    }*/

    void PrepareFullscreenPass () {
        Renderer.GL.Disable(EnableCap.ScissorTest);
        Renderer.GL.Disable(EnableCap.Blend);
        Renderer.GL.Disable(EnableCap.StencilTest);
        Renderer.GL.Disable(EnableCap.CullFace);
        Renderer.GL.Disable(EnableCap.DepthTest);
        Renderer.GL.ColorMask(true, true, true, true);
        Renderer.GL.DepthMask(false);
        Renderer.GL.Viewport(0, 0, (uint)_width, (uint)_height);
        Renderer.GL.Clear((uint)ClearBufferMask.ColorBufferBit);
    }

    void CopySceneDepth (uint targetFbo) {
        if (targetFbo != 0) return;

        Renderer.GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _sceneFbo);
        Renderer.GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, targetFbo);
        SetDrawBuffer(targetFbo);
        Renderer.GL.BlitFramebuffer(
            0, 0, _width, _height,
            0, 0, _width, _height,
            ClearBufferMask.DepthBufferBit,
            BlitFramebufferFilter.Nearest);
        Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, targetFbo);
    }

    void CopySceneColor (uint targetFbo) {
        Renderer.GL.Disable(EnableCap.ScissorTest);
        Renderer.GL.Disable(EnableCap.Blend);
        Renderer.GL.Disable(EnableCap.StencilTest);
        Renderer.GL.Disable(EnableCap.CullFace);
        Renderer.GL.Disable(EnableCap.DepthTest);
        Renderer.GL.ColorMask(true, true, true, true);
        Renderer.GL.DepthMask(false);

        Renderer.GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _sceneFbo);
        Renderer.GL.ReadBuffer(GLEnum.ColorAttachment0);
        Renderer.GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, targetFbo);
        SetDrawBuffer(targetFbo);
        Renderer.GL.BlitFramebuffer(
            0, 0, _width, _height,
            0, 0, _width, _height,
            ClearBufferMask.ColorBufferBit,
            BlitFramebufferFilter.Nearest);
        Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, targetFbo);
    }

    static void SetDrawBuffer (uint fbo) {
        Renderer.GL.DrawBuffer(fbo == 0 ? GLEnum.Back : GLEnum.ColorAttachment0);
    }

    void DeleteTargets () {
        DeleteFramebuffer(_sceneFbo);
        DeleteTexture(_sceneColor);
        DeleteRenderbuffer(_sceneDepth);

        for (int i = 0; i < 2; i++) {
            DeleteFramebuffer(_pingFbo[i]);
            DeleteTexture(_pingColor[i]);
            _pingFbo[i] = 0;
            _pingColor[i] = 0;
        }

        DeleteFramebuffer(_outputFbo);
        DeleteTexture(_outputColor);

        _sceneFbo = 0;
        _sceneColor = 0;
        _sceneDepth = 0;
        _outputFbo = 0;
        _outputColor = 0;
    }

    static void DeleteFramebuffer (uint id) {
        if (id != 0) Renderer.GL.DeleteFramebuffer(id);
    }

    static void DeleteTexture (uint id) {
        if (id != 0) Renderer.GL.DeleteTexture(id);
    }

    static void DeleteRenderbuffer (uint id) {
        if (id != 0) Renderer.GL.DeleteRenderbuffer(id);
    }

    public void Dispose () {
        DeleteTargets();
        if (QuadVAO != 0) {
            Renderer.GL.DeleteVertexArray(QuadVAO);
            QuadVAO = 0;
        }
    }
}
