using Silk.NET.OpenGL;
using Engine.Graphics.UI;
using static Engine.Graphics.Shader;
using static Engine.AssetsEngine;

namespace Engine.Graphics;

/// Camera Matrix
/// Skybox
/// Opaque
/// Transparent
/// PP
/// Gizmos
/// UI

public class Renderer {
    public Renderer () {
        Instance = this;

        if (Instance is not null && Instance != this)
            throw new Exception($"[ctor] {typeof(Renderer)}.{nameof(Instance)} ({GetHashCode()}) is not null");

        Engine.Instance.de_Render += Render;
        Windows.Window.FramebufferResize += OnFrameBufferResize;
        Windows.Window.Closing += Dispose;

        _GL = Windows.Window.CreateOpenGL();
        GLDebug.Init();
        GL.FrontFace(FrontFaceDirection.CW);
        GL.ClearColor(Constants.clearColor.X, Constants.clearColor.Y, Constants.clearColor.Z, 1f);

        Skybox = new Skybox(_hdr_Skybox);

        //SetTargetSize(Engine.Window.Size.X, Engine.Window.Size.Y);

        TextRenderer = new TextRenderer();

        PostProcess = new PostProcessStack();
        //PostProcess.Effects.Add(new PostProcessPass(_mat_Depth));
        //PostProcess.Effects.Add(new PostProcessPass(_mat_Grayscale));
        PostProcess.Effects.Add(new PostProcessPass(_mat_SSAO));
        PostProcess.Effects.Add(new PostProcessPass(_mat_SSAO_Blur));
        PostProcess.Effects.Add(new PostProcessPass(_mat_SSAO_Composite));
        //PostProcess.Effects.Add(new PostProcessPass(_mat_CameraFocus));
        PostProcess.Effects.Add(new PostProcessPass(_mat_Fxaa));
        //PostProcess.Effects.Add(new PostProcessPass(_mat_Vignette) { Enabled = false });


        /// Delegates
        de_DrawUI += TextRenderer.Draw;
    }

    public static Renderer Instance = null!;

    public Camera? Camera = null;

    //public Action? de_LateUpdate = null;

    public Action? de_PreRender = null;
    public Action? de_DrawPostScene = null;
    public Action? de_DrawAfterPostProcess = null;
    public Action? de_DrawUI = null;
    public Action? de_PostRender = null;


    protected readonly GL _GL = null!; /// set in ctor — don't give this a field initializer, it creates a second throwaway GL context
    public static GL GL => Instance._GL;

    public Action? de_Dispose = null;

    public readonly Skybox Skybox = null!;
    public readonly PostProcessStack PostProcess = null!;
    public readonly TextRenderer TextRenderer = null!;


    /// Debug
    public Matrix4x4 m4x4_View = Matrix4x4.Identity;
    public Matrix4x4 m4x4_Projection = Matrix4x4.Identity;
    public Matrix4x4 m4x4_ProjectionUI = Matrix4x4.Identity;

    protected readonly RenderState state = new RenderState();
    protected readonly RenderQueue queue = new RenderQueue();
    protected Frustum frustum = new Frustum(); /// not readonly — a readonly struct field makes a defensive copy on every call

    protected Matrix4x4[] instanceModelScratch = [];

    public RendererStats Stats = new RendererStats();
    public int Width => (int)MathF.Round(Stats.SceneSize.X);
    public int Height => (int)MathF.Round(Stats.SceneSize.Y);

    protected System.Diagnostics.Stopwatch sw_Latency = new System.Diagnostics.Stopwatch();

    /// Cached so UpdateViewProjection only rebuilds the UI ortho matrix when size actually changes
    protected float lastProjWidth = -1f;
    protected float lastProjHeight = -1f;



    public virtual void Render () {
        ClearFrame();

        /// Render
        Camera = MainCamera;
        if (Camera is null) {
            Log.log($"No {nameof(Graphics.Camera)} found");
            queue.Clear(); /// otherwise the queue grows every frame while there is no camera
            return;
        }
        StatsStart();

        de_PreRender?.Invoke();

        SetTargetSize();

        PostProcess.Resize(Width, Height);

        /// Camera Matrix
        UpdateViewProjection(Width, Height);

        PostProcess.BeginScene();

        Skybox.Draw();

        DrawSceneAll();

        //SceneManager.ActiveScene?.DrawRaw();

        PostProcess.Run();

        de_DrawPostScene?.Invoke();

        //PostProcess.BindOutputForOverlay();

        de_DrawAfterPostProcess?.Invoke();

        /// UI Stage
        de_DrawUI?.Invoke();
        //ComponentManager.Instance.DrawRaw();

        PresentToBackbuffer();

        de_PostRender?.Invoke();

        Stats.Frame++;

        StatsEnd();

        //Log.log("Renderer", Stats.Frame, Windows.Window.Size, Stats.SceneSize);
        //Thread.Sleep(500);
    }
    public void ClearFrame () {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
    }
    protected void StatsStart () {
        sw_Latency.Restart();

        Stats.DrawCalls = 0;
        Stats.PostProccessCalls = 0;
        Stats.DrawCallsUI = 0;
        Stats.WindowSize = new Vector2(Windows.Window.Size.X, Windows.Window.Size.Y);
        Stats.SceneSize = Stats.WindowSize;

        state.Reset();
    }
    protected void StatsEnd () {
        Stats.Latency = (float)sw_Latency.Elapsed.TotalMilliseconds;

        queue.Clear();
    }
    public virtual void SetTargetSize () {
        Stats.SceneSize = new Vector2(Windows.Window.Size.X, Windows.Window.Size.Y);
    }
    public virtual void PresentToBackbuffer () {
        PostProcess.PresentToBackbuffer(); /// blit _outputFbo into fbo 0
    }

    public void AddRenderInfo (RenderInfo renderInfo) {
        queue.Add(renderInfo);
    }

    protected void UpdateViewProjection (float width, float height) {
        m4x4_View = Camera!.GetViewMatrix();

        float aspect = width/height;
        m4x4_Projection = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(
            Camera.FOV*Mathf.Deg2Rad, aspect, Camera.PlaneNear, Camera.PlaneFar);

        /// Ortho only depends on width/height, not the camera — skip rebuilding it every frame
        if (width != lastProjWidth || height != lastProjHeight) {
            m4x4_ProjectionUI = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, -1f, 1f);
            lastProjWidth = width;
            lastProjHeight = height;
        }
    }
    protected virtual Camera? MainCamera => Camera.Main;

    protected virtual void DrawSceneAll () {
        switch (Constants.drawMode) {
            case DrawMode.Normal:
                DrawScene();
                break;
        }
    }
    protected void DrawScene () {
        //Log.log("Objects");
        //int c = SceneManager.ActiveScene.Objects.Count;
        //for (int i = 0; i < c; i++) {
        //    Log.log(SceneManager.ActiveScene.Objects[i].Name);
        //}
        //DrawSame();
        //Log.log("RenderList", RenderList.Count);
        //int s = 0;
        //Log.log("depth", Renderer.GL.IsEnabled(EnableCap.DepthTest), Renderer.GL.GetBoolean(GetPName.DepthWritemask));

        frustum.Extract(m4x4_View*m4x4_Projection);
        long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        queue.Build(frustum, Camera!.CameraPos);
        long t1 = System.Diagnostics.Stopwatch.GetTimestamp();
        /// Consecutive runs of the same (mesh, material) — adjacent thanks to the sort key —
        /// get drawn as one instanced call. Every shader used here must read the model/normal
        /// matrix from the instanced attributes (locations 3 and 7) — see Mesh.DrawInstanced.
        /// UI is the exception: it goes through DrawRenderInfo with uniforms.
        int count = queue.Count;
        int idx = 0;
        bool normalsOn = true;
        //Log.log("Start");
        while (idx < count) {
            RenderInfo first = queue[idx];

            /// Everything after the opaque pass (transparents, UI) doesn't write FragNormal
            if (normalsOn && first.material.Pass != RenderPass.Opaque) {
                PostProcess.EndNormalOutput();
                //Log.log("EndNormalOutput");
                normalsOn = false;
            }
            //Log.log("RenderInfo", first.material.Pass);

            if (first.material.Pass == RenderPass.UI) {
                DrawRenderInfo(first);
                idx++;
                continue;
            }

            int runEnd = queue.RunEnd(idx);
            DrawInstancedRun(idx, runEnd);
            idx = runEnd;
        }
        long t2 = System.Diagnostics.Stopwatch.GetTimestamp();
        //Log.log("build ms", System.Diagnostics.Stopwatch.GetElapsedTime(t0, t1).TotalMilliseconds,
        //    "draw ms", System.Diagnostics.Stopwatch.GetElapsedTime(t1, t2).TotalMilliseconds);
    }

    /// Applies material GL state, shader, per-pass uniforms and material textures.
    /// Shared by DrawRenderInfo and DrawInstancedRun.
    public void BindMaterial (Material material) {
        Shader shader = state.Bind(material);

        /// Kept outside the shader-changed check on purpose: the skybox binds Texture0,
        /// and material.Apply() may rebind it, so this runs on every bind
        switch (material.Pass) {
            case RenderPass.Opaque:
            case RenderPass.Transparent:
                SetSceneUniformsUnlit(shader, Camera!.CameraPos);
                SetSceneUniformsLit(shader);
                SetSceneUniformsSkybox(shader, Skybox.prefilteredHandle, Skybox.maxLod);
                break;
            case RenderPass.UI:
                shader.SetMatrix4x4(uProjection, m4x4_ProjectionUI);
                break;
        }

        material.Apply();
    }

    public void DrawRenderInfo (RenderInfo info) {
        if (Camera is null || info.mesh is null || info.material is null) return;

        BindMaterial(info.material);

        Shader shader = info.material.shader;
        shader.SetMatrix4x4(uModel, info.model);

        info.mesh.Draw(info.indexOffset, info.indexCount, info.primitiveType);
    }

    /// Draws queue items [startIndex, endIndexExclusive) as one instanced callf
    protected void DrawInstancedRun (int startIndex, int endIndexExclusive) {
        int runLength = endIndexExclusive - startIndex;
        if (instanceModelScratch.Length < runLength) {
            instanceModelScratch = new Matrix4x4[runLength];
        }

        RenderInfo first = queue[startIndex];

        for (int i = 0; i < runLength; i++) {
            RenderInfo info = queue[startIndex + i];
            instanceModelScratch[i] = info.model;
        }

        BindMaterial(first.material);

        first.mesh.DrawInstanced(new ReadOnlySpan<Matrix4x4>(instanceModelScratch, 0, runLength),
            first.indexOffset, first.indexCount, first.primitiveType);
    }


    public void SetSceneUniformsUnlit (Shader shader, Vector3 viewPos) {
        shader.SetMatrix4x4(uView, m4x4_View);
        shader.SetMatrix4x4(uProjection, m4x4_Projection);
        shader.SetVector3(uViewPos, viewPos);
    }
    public static void SetSceneUniformsLit (Shader shader) {
        if (!shader.isLit) return;
        Lighting.SetSceneUniformsLit(shader);
    }
    public static void SetSceneUniformsSkybox (Shader shader, uint prefilteredHandle, float maxLod) {
        if (!Constants.renderSkyboxReflection) return;
        if (prefilteredHandle == 0) return;

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, prefilteredHandle);
        shader.SetInt(Shader.uSkybox, 0);
        shader.SetFloat(uMaxReflectionLod, maxLod);
    }

    protected void OnFrameBufferResize (Silk.NET.Maths.Vector2D<int> newSize) {
        GL.Viewport(newSize);
    }

    protected virtual void Dispose () {
        Engine.Instance.de_Render -= Render;
        Windows.Window.FramebufferResize -= OnFrameBufferResize;
        Windows.Window.Closing -= Dispose;

        TextRenderer.Dispose();

        Skybox.Dispose();
        PostProcess.Dispose();

        de_Dispose?.Invoke();
    }

}