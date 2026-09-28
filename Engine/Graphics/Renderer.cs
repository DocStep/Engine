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
        PostProcess.Effects.Add(new PostProcessPass(_mat_SSAOBlur));
        PostProcess.Effects.Add(new PostProcessPass(_mat_SSAOComposite));
        //PostProcess.Effects.Add(new PostProcessPass(_mat_CameraFocus));
        PostProcess.Effects.Add(new PostProcessPass(_mat_Fxaa));
        PostProcess.Effects.Add(new PostProcessPass(_mat_Vignette) { Enabled = false });


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

    protected readonly RenderState _state = new RenderState();
    protected readonly RenderQueue _queue = new RenderQueue();
    protected Frustum _frustum = new Frustum(); /// not readonly — a readonly struct field makes a defensive copy on every call

    protected Matrix4x4[] _instanceModelScratch = Array.Empty<Matrix4x4>();
    protected Matrix4x4[] _instanceNormalScratch = Array.Empty<Matrix4x4>();

    public RendererStats Stats = new RendererStats();
    public int Width => (int)MathF.Round(Stats.SceneSize.X);
    public int Height => (int)MathF.Round(Stats.SceneSize.Y);

    protected System.Diagnostics.Stopwatch sw_Latency = new System.Diagnostics.Stopwatch();

    /// Cached so UpdateViewProjection only rebuilds the UI ortho matrix when size actually changes
    protected float _lastProjWidth = -1f;
    protected float _lastProjHeight = -1f;



    public virtual void Render () {
        /// Clear Frame
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

        /// Render
        Camera = MainCamera;
        if (Camera is null) {
            Log.log($"No {nameof(Graphics.Camera)} found");
            _queue.Clear(); /// otherwise the queue grows every frame while there is no camera
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
    protected void StatsStart () {
        sw_Latency.Restart();

        Stats.DrawCalls = 0;
        Stats.PostProccessCalls = 0;
        Stats.DrawCallsUI = 0;
        Stats.WindowSize = new Vector2(Windows.Window.Size.X, Windows.Window.Size.Y);
        Stats.SceneSize = Stats.WindowSize;

        _state.Reset();
    }
    protected void StatsEnd () {
        Stats.Latency = (float)sw_Latency.Elapsed.TotalMilliseconds;

        _queue.Clear();
    }
    public virtual void SetTargetSize () {
        Stats.SceneSize = new Vector2(Windows.Window.Size.X, Windows.Window.Size.Y);
    }
    public virtual void PresentToBackbuffer () {
        PostProcess.PresentToBackbuffer(); /// blit _outputFbo into fbo 0
    }

    public void AddRenderInfo (RenderInfo renderInfo) {
        _queue.Add(renderInfo);
    }

    protected void UpdateViewProjection (float width, float height) {
        m4x4_View = Camera!.GetViewMatrix();

        float aspect = width/height;
        m4x4_Projection = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(
            Camera.FOV*Mathf.Deg2Rad, aspect, Camera.PlaneNear, Camera.PlaneFar);

        /// Ortho only depends on width/height, not the camera — skip rebuilding it every frame
        if (width != _lastProjWidth || height != _lastProjHeight) {
            m4x4_ProjectionUI = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, -1f, 1f);
            _lastProjWidth = width;
            _lastProjHeight = height;
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

        _frustum.Extract(m4x4_View*m4x4_Projection);
        long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        _queue.Build(_frustum, Camera!.CameraPos);
        long t1 = System.Diagnostics.Stopwatch.GetTimestamp();
        /// Consecutive runs of the same (mesh, material) — adjacent thanks to the sort key —
        /// get drawn as one instanced call. Every shader used here must read the model/normal
        /// matrix from the instanced attributes (locations 3 and 7) — see Mesh.DrawInstanced.
        /// UI is the exception: it goes through DrawRenderInfo with uniforms.
        int count = _queue.Count;
        int idx = 0;
        while (idx < count) {
            RenderInfo first = _queue[idx];

            if (first.material.Pass == RenderPass.UI) {
                DrawRenderInfo(first);
                idx++;
                continue;
            }

            int runEnd = _queue.RunEnd(idx);
            DrawInstancedRun(idx, runEnd);
            idx = runEnd;
        }
        long t2 = System.Diagnostics.Stopwatch.GetTimestamp();
        Log.log("build ms", System.Diagnostics.Stopwatch.GetElapsedTime(t0, t1).TotalMilliseconds,
            "draw ms", System.Diagnostics.Stopwatch.GetElapsedTime(t1, t2).TotalMilliseconds);
    }

    /// Applies material GL state, shader, per-pass uniforms and material textures.
    /// Shared by DrawRenderInfo and DrawInstancedRun.
    protected void BindMaterial (Material material) {
        Shader shader = _state.Bind(material);

        /// Kept outside the shader-changed check on purpose: the skybox binds Texture0,
        /// and material.Apply() may rebind it, so this runs on every bind
        switch (material.Pass) {
            case RenderPass.Opaque:
            case RenderPass.Transparent:
                SetSceneUniformsUnlit(shader, Camera!.CameraPos);
                SetSceneUniformsLit(shader);
                SetSceneUniformsSkybox(shader, Skybox.texture, Skybox.maxLod);
                break;
            case RenderPass.UI:
                shader.SetMatrix4x4(Projection, m4x4_ProjectionUI);
                break;
        }

        material.Apply();
    }

    public void DrawRenderInfo (RenderInfo info) {
        if (Camera is null) return;
        if (info.mesh is null) return;
        if (info.material is null) return;

        BindMaterial(info.material);

        Shader shader = info.material.shader;
        shader.SetMatrix4x4(Model, info.model);
        shader.SetMatrix4x4(NormalMatrix, info.normal ?? GetNormalMatrix(info.model));

        info.mesh.Draw(info.indexOffset, info.indexCount, info.primitiveType);
    }

    /// Draws queue items [startIndex, endIndexExclusive) as one instanced call
    protected void DrawInstancedRun (int startIndex, int endIndexExclusive) {
        int runLength = endIndexExclusive - startIndex;
        if (_instanceModelScratch.Length < runLength) {
            _instanceModelScratch = new Matrix4x4[runLength];
            _instanceNormalScratch = new Matrix4x4[runLength];
        }

        RenderInfo first = _queue[startIndex];

        for (int i = 0; i < runLength; i++) {
            RenderInfo info = _queue[startIndex + i];
            _instanceModelScratch[i] = info.model;
            _instanceNormalScratch[i] = info.normal ?? GetNormalMatrix(info.model);
        }

        BindMaterial(first.material);

        first.mesh.DrawInstanced(
            new ReadOnlySpan<Matrix4x4>(_instanceModelScratch, 0, runLength),
            new ReadOnlySpan<Matrix4x4>(_instanceNormalScratch, 0, runLength),
            first.indexOffset, first.indexCount,
            first.primitiveType);
    }

    /// A full inverse-transpose is only needed for non-uniform scale. Uniform scale cancels out,
    /// so we can skip the Matrix4x4.Invert (the expensive part) in the common case.
    protected static Matrix4x4 GetNormalMatrix (Matrix4x4 model) {
        float sx = model.M11*model.M11 + model.M12*model.M12 + model.M13*model.M13;
        float sy = model.M21*model.M21 + model.M22*model.M22 + model.M23*model.M23;
        float sz = model.M31*model.M31 + model.M32*model.M32 + model.M33*model.M33;

        bool uniformScale = MathF.Abs(sx - sy) < 0.0001f && MathF.Abs(sy - sz) < 0.0001f;
        if (uniformScale) return model;

        if (!Matrix4x4.Invert(model, out Matrix4x4 inverseModel))
            inverseModel = Matrix4x4.Identity; /// fallback — model scale is degenerate, fix at the source (clamp scale.y)
        return Matrix4x4.Transpose(inverseModel);
    }


    public void SetSceneUniformsUnlit (Shader shader, Vector3 viewPos) {
        shader.SetMatrix4x4(View, m4x4_View);
        shader.SetMatrix4x4(Projection, m4x4_Projection);
        shader.SetVector3(ViewPos, viewPos);
    }
    public static void SetSceneUniformsLit (Shader shader) {
        if (!shader.isLit) return;
        Lighting.SetSceneUniformsLit(shader);
    }
    public static void SetSceneUniformsSkybox (Shader shader, HdrTexture? texture, float maxLod) {
        if (!Constants.renderSkyboxReflection) return;
        if (texture is null) return;

        texture.Bind(TextureUnit.Texture0);
        shader.SetInt(Shader.Skybox, 0);
        shader.SetFloat(MaxReflectionLod, maxLod);
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