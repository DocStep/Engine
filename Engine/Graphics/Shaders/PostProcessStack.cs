using Silk.NET.OpenGL;
using static Engine.Graphics.Shader;
using static Engine.AssetsEngine;

namespace Engine.Graphics;


public class PostProcessStack : IDisposable {
    public PostProcessStack () {
        QuadVAO = Renderer.GL.GenVertexArray();
        Windows.Window.FramebufferResize += Resize;
        Engine.Instance.de_Update += Update;

        Resize(Renderer.Instance.Width, Renderer.Instance.Height);
    }

    public static uint QuadVAO;

    public bool Enabled = true;
    public List<PostProcessPass> Effects = new();

    int _width, _height;

    /// Final Result
    uint _sceneFbo, _sceneColor, _sceneDepth, _pingDepth0, _pingDepth1;
    uint[] _pingFbo = new uint[2];
    uint[] _pingColor = new uint[2];

    uint _outputFbo, _outputColor, _outputDepth;

    public uint SceneColorTexture => _sceneColor;
    public uint OutputTexture => _outputColor;


    public void Update () {
        if (Input.Inputs.Actions[Input.Inputs.PP].pressedDown) {
            Enabled = !Enabled;
        }
    }

    public void Resize (int width, int height) {
        if (width <= 0 || height <= 0) return;
        if (_width == width && _height == height && _sceneFbo != 0) return;

        DeleteTargets();

        _width = width;
        _height = height;

        _sceneFbo = CreateFbo(width, height, out _sceneColor, out _sceneDepth, withDepth: true);
        _pingFbo[0] = CreateFbo(width, height, out _pingColor[0], out _pingDepth0, withDepth: true);
        _pingFbo[1] = CreateFbo(width, height, out _pingColor[1], out _pingDepth1, withDepth: true);

        _outputFbo = CreateFbo(width, height, out _outputColor, out _outputDepth, withDepth: true);
    }
    public void Resize (Silk.NET.Maths.Vector2D<int> newSize) => Resize(newSize.X, newSize.Y);

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

    /// Call before drawing the scene
    public void BeginScene () {
        Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, _sceneFbo);
        SetDrawBuffer(_sceneFbo);
        Renderer.GL.Viewport(0, 0, (uint)Renderer.Instance.Width, (uint)Renderer.Instance.Height);
        //Log.log("PPS Viewport", (uint)Renderer.Instance.Width, (uint)Renderer.Instance.Height);
        Renderer.GL.ColorMask(true, true, true, true);
        Renderer.GL.DepthMask(true);
        Renderer.GL.DepthFunc(DepthFunction.Less);
        Renderer.GL.Disable(EnableCap.StencilTest);
        Renderer.GL.Disable(EnableCap.Blend);
        Renderer.GL.Enable(EnableCap.DepthTest);
        Renderer.GL.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
    }

    /// Bind the output FBO so gizmos/text/debug draws land inside the scene texture, not the window
    public void BindOutputForOverlay () {
        Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, _outputFbo);
        SetDrawBuffer(_outputFbo);
        Renderer.GL.Viewport(0, 0, (uint)Renderer.Instance.Width, (uint)Renderer.Instance.Height);

        // Restore depth testing so overlays (gizmos, text) can depth-test against the scene
        // but don't write depth so we don't modify the copied scene depth buffer.
        //Renderer.GL.Enable(EnableCap.DepthTest);
        //Renderer.GL.DepthFunc(DepthFunction.Lequal);
        //Renderer.GL.DepthMask(false);
        //Renderer.GL.ColorMask(true, true, true, true);
    }

    public void Run () => Run(_outputFbo);
    public void Run (uint finalTargetFbo) {
        Renderer.GL.Disable(EnableCap.DepthTest);

        if (Enabled && 0 < Effects.Count) {
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
        } else {
            CopySceneColor(finalTargetFbo);
            CopySceneDepth(finalTargetFbo);
        }

        ForceOpaqueAlpha(finalTargetFbo);
        Renderer.GL.DepthMask(true);
    }

    public void PresentToBackbuffer () {
        Renderer.GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _outputFbo);
        Renderer.GL.ReadBuffer(GLEnum.ColorAttachment0);
        Renderer.GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        Renderer.GL.DrawBuffer(GLEnum.Back);
        Renderer.GL.BlitFramebuffer(0, 0, _width, _height, 0, 0, Renderer.Instance.Width, Renderer.Instance.Height,
            ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
        Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }


    /// Writes alpha = 1 across finalTargetFbo without touching RGB. Unlike BlitFramebuffer,
    /// GL.Clear respects GL.ColorMask, so this reliably scrubs whatever partial alpha
    /// transparent scene draws left behind, regardless of which path filled the target.
    void ForceOpaqueAlpha (uint targetFbo) {
        //Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, targetFbo);
        ///SetDrawBuffer(targetFbo);
        Renderer.GL.ColorMask(false, false, false, true);
        //Renderer.GL.ClearColor(0f, 0f, 0f, 1f);
        Renderer.GL.Clear((uint)ClearBufferMask.ColorBufferBit);
        Renderer.GL.ColorMask(true, true, true, true);
        ///Renderer.GL.ClearColor(Constants.clearColor.X, Constants.clearColor.Y, Constants.clearColor.Z, 1f);
    }

    void PrepareFullscreenPass () {
        Renderer.GL.Disable(EnableCap.ScissorTest);
        Renderer.GL.Disable(EnableCap.Blend);
        Renderer.GL.Disable(EnableCap.StencilTest);
        Renderer.GL.Disable(EnableCap.CullFace);
        Renderer.GL.Disable(EnableCap.DepthTest);
        Renderer.GL.ColorMask(true, true, true, true);
        Renderer.GL.DepthMask(false);
        Renderer.GL.Viewport(0, 0, (uint)Renderer.Instance.Width, (uint)Renderer.Instance.Height);
        Renderer.GL.Clear((uint)ClearBufferMask.ColorBufferBit);
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
            0, 0, Renderer.Instance.Width, Renderer.Instance.Height,
            0, 0, Renderer.Instance.Width, Renderer.Instance.Height,
            ClearBufferMask.ColorBufferBit,
            BlitFramebufferFilter.Nearest);
        Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, targetFbo);
    }
    void CopySceneDepth (uint targetFbo) {
        Renderer.GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _sceneFbo);
        Renderer.GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, targetFbo);
        SetDrawBuffer(targetFbo);
        Renderer.GL.BlitFramebuffer(
            0, 0, Renderer.Instance.Width, Renderer.Instance.Height,
            0, 0, Renderer.Instance.Width, Renderer.Instance.Height,
            ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit,
            BlitFramebufferFilter.Nearest);
        Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, targetFbo);
    }


    static void SetDrawBuffer (uint fbo) {
        Renderer.GL.DrawBuffer(fbo == 0 ? GLEnum.Back : GLEnum.ColorAttachment0);
    }



    void DeleteTargets () {
        DeleteFramebuffer(_sceneFbo);
        DeleteTexture(_sceneColor);
        DeleteTexture(_sceneDepth);

        for (int i = 0; i < 2; i++) {
            DeleteFramebuffer(_pingFbo[i]);
            DeleteTexture(_pingColor[i]);
            _pingFbo[i] = 0;
            _pingColor[i] = 0;
        }

        DeleteFramebuffer(_outputFbo);
        DeleteTexture(_outputColor);
        DeleteTexture(_outputDepth);
        DeleteTexture(_pingDepth0);
        DeleteTexture(_pingDepth1);

        _sceneFbo = 0;
        _sceneColor = 0;
        _sceneDepth = 0;
        _outputFbo = 0;
        _outputColor = 0;
        _outputDepth = 0;
        _pingDepth0 = 0;
        _pingDepth1 = 0;
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

    public void DebugReadDepth (uint fbo, int x, int y) {
        Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
        float depth = 0f;
        unsafe {
            Renderer.GL.ReadPixels(x, y, 1, 1, PixelFormat.DepthComponent, PixelType.Float, &depth);
        }
        Log.log($"depth@({x},{y}) fbo={fbo}: {depth}");
        Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }


    public void Dispose () {
        DeleteTargets();
        if (QuadVAO != 0) {
            Renderer.GL.DeleteVertexArray(QuadVAO);
            QuadVAO = 0;
        }
    }

}
