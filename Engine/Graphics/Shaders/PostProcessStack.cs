using Silk.NET.OpenGL;
using static Engine.Graphics.Shader;
using static Engine.AssetsEngine;

namespace Engine.Graphics;


public class PostProcessStack {
    public static uint QuadVAO;

    uint _sceneFbo, _sceneColor, _sceneDepth;
    uint[] _pingFbo = new uint[2];
    uint[] _pingColor = new uint[2];

    uint _outputFbo, _outputColor; /// final result, this is what Scene View samples
    public uint OutputTexture => _outputColor;

    public List<PostProcessEffect> Effects = new();

    public PostProcessStack () {
        QuadVAO = Renderer.GL.GenVertexArray();

        int w = Engine.Window.Size.X;
        int h = Engine.Window.Size.Y;

        _sceneFbo = CreateFbo(w, h, out _sceneColor, out _sceneDepth, withDepth: true);
        _pingFbo[0] = CreateFbo(w, h, out _pingColor[0], out _, withDepth: false);
        _pingFbo[1] = CreateFbo(w, h, out _pingColor[1], out _, withDepth: false);
        _outputFbo = CreateFbo(w, h, out _outputColor, out _, withDepth: false);
    }

    uint CreateFbo (int w, int h, out uint colorTex, out uint depthRbo, bool withDepth) {
        uint fbo = Renderer.GL.GenFramebuffer();
        Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

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

        depthRbo = 0;
        if (withDepth) {
            depthRbo = Renderer.GL.GenRenderbuffer();
            Renderer.GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthRbo);
            Renderer.GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, (uint)w, (uint)h);
            Renderer.GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment,
                RenderbufferTarget.Renderbuffer, depthRbo);
        }

        var status = Renderer.GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
            Log.log($"PostProcess FBO incomplete: {status}");

        Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        return fbo;
    }
    public void EndSceneAndRunStack () {
        Renderer.GL.Disable(EnableCap.DepthTest);

        if (Effects.Count == 0) {
            Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, _outputFbo);
            Blit(_sceneColor);
            return;
        }

        uint currentInput = _sceneColor;
        int pingIndex = 0;

        for (int i = 0; i < Effects.Count; i++) {
            bool isLast = i == Effects.Count - 1;
            Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer,
                isLast ? _outputFbo : _pingFbo[pingIndex]);

            Effects[i].Apply(currentInput);

            currentInput = _pingColor[pingIndex];
            pingIndex = 1 - pingIndex;
        }
    }


    /// Call before drawing the scene
    public void BeginScene () {
        Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, _sceneFbo);
        Renderer.GL.Enable(EnableCap.DepthTest);
        Renderer.GL.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
    }

    /// Call after all scene draws (skybox, meshes, etc). Runs the whole stack, ends drawing to screen (fbo 0).
    public void EndSceneAndRunStack (uint finalTargetFbo) {
        Renderer.GL.Disable(EnableCap.DepthTest);

        if (Effects.Count == 0) {
            Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer, finalTargetFbo);
            Blit(_sceneColor);
            return;
        }

        uint currentInput = _sceneColor;
        int pingIndex = 0;

        for (int i = 0; i < Effects.Count; i++) {
            bool isLast = i == Effects.Count - 1;
            Renderer.GL.BindFramebuffer(FramebufferTarget.Framebuffer,
                isLast ? finalTargetFbo : _pingFbo[pingIndex]);

            Effects[i].Apply(currentInput);

            currentInput = _pingColor[pingIndex];
            pingIndex = 1 - pingIndex;
        }
    }

    void Blit (uint tex) {
        Renderer.GL.Disable(EnableCap.ScissorTest);
        Renderer.GL.Viewport(0, 0, (uint)Engine.Window.Size.X, (uint)Engine.Window.Size.Y);
        Renderer.GL.ClearColor(0f, 1f, 0f, 1f); // green
        Renderer.GL.Clear((uint)ClearBufferMask.ColorBufferBit);
        return;

        Renderer.GL.Disable(EnableCap.ScissorTest);
        Renderer.GL.Disable(EnableCap.Blend);
        Renderer.GL.Viewport(0, 0, (uint)Engine.Window.Size.X, (uint)Engine.Window.Size.Y);
        Renderer.GL.Clear((uint)ClearBufferMask.ColorBufferBit);

        _sh_Passthrough.Use();
        Renderer.GL.ActiveTexture(TextureUnit.Texture0);
        Renderer.GL.BindTexture(TextureTarget.Texture2D, tex);
        _sh_Passthrough.SetInt("uScene", 0);

        Renderer.GL.BindVertexArray(QuadVAO);
        Renderer.GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        Renderer.Instance.Stats.DrawCalls++;
        Renderer.GL.BindVertexArray(0);
    }

}
