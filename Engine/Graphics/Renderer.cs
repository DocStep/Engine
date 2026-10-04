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

        gl = Windows.Window.CreateOpenGL();
        GLDebug.Init();
        gl.FrontFace(FrontFaceDirection.CW);
        gl.ClearColor(Constants.clearColor.X, Constants.clearColor.Y, Constants.clearColor.Z, 1f);

        Skybox = new Skybox(_hdr_Skybox);

        //SetTargetSize(Windows.Window.Size.X, Windows.Window.Size.Y);

        Shadow = new ShadowMap(2048);
        PointShadows = new PointShadowMap();

        PostProcess = new PostProcessStack();
        //PostProcess.Effects.Add(new PostProcessPass(_mat_Depth));
        //PostProcess.Effects.Add(new PostProcessPass(_mat_Grayscale));
        PostProcess.Effects.Add(new PostProcessPass(_mat_SSAO));
        PostProcess.Effects.Add(new PostProcessPass(_mat_SSAO_Blur));
        PostProcess.Effects.Add(new PostProcessPass(_mat_SSAO_Composite));
        //PostProcess.Effects.Add(new PostProcessPass(_mat_CameraFocus));
        PostProcess.Effects.Add(new PostProcessPass(_mat_Fxaa));
        //PostProcess.Effects.Add(new PostProcessPass(_mat_Vignette) { Enabled = false });

        TextRenderer = new TextRenderer();


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


    protected readonly GL gl = null!; /// set in ctor — don't give this a field initializer, it creates a second throwaway GL context
    public static GL GL => Instance.gl;

    public Action? de_Dispose = null;

    public readonly Skybox Skybox = null!;
    public ShadowMap Shadow = null!;
    public PointShadowMap PointShadows = null!;
    public SunLight? SunShadowLight; /// the sun the map was actually rendered for this frame
    protected readonly RenderQueue shadowQueue = new RenderQueue();
    protected Frustum lightFrustum = new Frustum();
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
            ClearRenderData();
            return;
        }
        StatsStart();

        de_PreRender?.Invoke();

        SetTargetSize();

        PostProcess.Resize(Width, Height);

        /// Camera Matrix
        UpdateViewProjection(Width, Height);

        DrawShadowPass();

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

        ClearRenderData();

        //Log.log("Renderer", Stats.Frame, Windows.Window.Size, Stats.SceneSize);
        //Thread.Sleep(500);
    }
    public void ClearFrame () {
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
    }
    public void ClearRenderData () {
        queue.Clear();
        shadowQueue.Clear();

        StatsEnd();
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
    }
    public virtual void SetTargetSize () {
        Stats.SceneSize = new Vector2(Windows.Window.Size.X, Windows.Window.Size.Y);
    }
    public virtual void PresentToBackbuffer () {
        PostProcess.PresentToBackbuffer(); /// blit _outputFbo into fbo 0
    }

    public void AddRenderData (RenderData data) {
        queue.Add(data);
        shadowQueue.Add(data);
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
            RenderData first = queue[idx];

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

    public void DrawRenderInfo (RenderData info) {
        if (Camera is null || info.mesh is null || info.material is null) return;

        BindMaterial(info.material);

        Shader shader = info.material.shader;
        shader.SetMatrix4x4(uModel, info.model);

        info.mesh.Draw(info.indexOffset, info.indexCount, info.primitiveType);
    }

    /// Draws queue items [startIndex, endIndexExclusive) as one instanced call
    protected void DrawInstancedRun (int startIndex, int endIndexExclusive) {
        int runLength = endIndexExclusive - startIndex;
        if (instanceModelScratch.Length < runLength) {
            instanceModelScratch = new Matrix4x4[runLength];
        }

        RenderData first = queue[startIndex];

        for (int i = 0; i < runLength; i++) {
            RenderData info = queue[startIndex + i];
            instanceModelScratch[i] = info.model;
        }

        BindMaterial(first.material);

        first.mesh.DrawInstanced(new ReadOnlySpan<Matrix4x4>(instanceModelScratch, 0, runLength),
            first.indexOffset, first.indexCount, first.primitiveType);
    }

    /// Draws shadowQueue items [startIndex, endIndexExclusive) as one instanced depth-only call
    protected void DrawShadowRun (int startIndex, int endIndexExclusive) {
        int runLength = endIndexExclusive - startIndex;
        if (instanceModelScratch.Length < runLength) {
            instanceModelScratch = new Matrix4x4[runLength];
        }

        RenderData first = shadowQueue[startIndex];

        for (int i = 0; i < runLength; i++) {
            instanceModelScratch[i] = shadowQueue[startIndex + i].model;
        }

        /// Depth material instead of first.material: no scene uniforms, no material textures
        Shader shader = state.Bind(_mat_ShadowDepth);
        shader.SetMatrix4x4(Shader.uLightSpace, Shadow.LightSpace);

        /// state.Bind may apply the depth material's own cull mode, so force front-face culling after it
        gl.Enable(EnableCap.CullFace);
        gl.CullFace(TriangleFace.Back);

        first.mesh.DrawInstanced(new ReadOnlySpan<Matrix4x4>(instanceModelScratch, 0, runLength),
            first.indexOffset, first.indexCount, first.primitiveType);
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
                Lighting.SetSceneUniformsLit(shader);
                SetSceneUniformsSkybox(shader, Skybox.prefilteredHandle, Skybox.maxLod);
                break;
            case RenderPass.UI:
                shader.SetMatrix4x4(uProjection, m4x4_ProjectionUI);
                break;
        }

        material.Apply();
    }

    public void SetSceneUniformsUnlit (Shader shader, Vector3 viewPos) {
        shader.SetMatrix4x4(uView, m4x4_View);
        shader.SetMatrix4x4(uProjection, m4x4_Projection);
        shader.SetVector3(uViewPos, viewPos);
    }
    public static void SetSceneUniformsSkybox (Shader shader, uint prefilteredHandle, float maxLod) {
        if (!Constants.renderSkyboxReflection) return;
        if (prefilteredHandle == 0) return;

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, prefilteredHandle);
        shader.SetInt(Shader.uSkybox, 0);
        shader.SetFloat(uMaxReflectionLod, maxLod);
    }


    protected void DrawShadowPass () {
        DrawShadowPasses();
        PostProcess.BeginScene();
    }

    void DrawShadowPasses () {
        SunShadowLight = null;
        Array.Clear(PointShadows.Slots);
        if (!Lighting.UseShadow) return;

        SunLight? sun = Lighting.GetShadowSun();
        if (sun is not null) {
            Shadow.SetSun(sun.gameObject.Transform.Forward, Camera!.CameraPos, 35f, 100f, 0.05f);
            lightFrustum.Extract(Shadow.LightSpace);
            shadowQueue.Build(lightFrustum, Camera.CameraPos);

            Shadow.Begin();
            Shader shader = BindDepthMaterial(_mat_ShadowDepth, true);
            shader.SetMatrix4x4("uLightSpace", Shadow.LightSpace);
            DrawDepthQueue(shadowQueue);
            Shadow.End();
            SunShadowLight = sun;
        }

        DrawPointShadowPass(); /// unchanged

        GL.CullFace(TriangleFace.Back);
        state.Reset();
    }

    void DrawSunShadowPass () {
        SunLight? light = Lighting.GetShadowSun();
        if (light is null) return;

        Vector3 sunDir = light.gameObject.Transform.Forward;
        Shadow.SetSun(sunDir, Camera!.CameraPos, 35f, 100f, 0.05f);

        lightFrustum.Extract(Shadow.LightSpace);
        shadowQueue.Build(lightFrustum, Camera.CameraPos);

        Shadow.Begin();
        Shader shader = BindDepthMaterial(_mat_ShadowDepth, true);
        shader.SetMatrix4x4("uLightSpace", Shadow.LightSpace);
        DrawDepthQueue(shadowQueue);
        Shadow.End();

        SunShadowLight = light;
    }

    void DrawPointShadowPass () {
        Lighting.PickPointShadowLights(PointShadows.Slots, Camera!.CameraPos);
        PointShadows.Begin();

        for (int slot = 0; slot < PointShadowMap.MaxLights; slot++) {
            PointLight? l = PointShadows.Slots[slot];
            if (l is null) continue;

            for (int face = 0; face < 6; face++) {
                Matrix4x4 vp = PointShadowMap.FaceMatrix(l.Position, l.Range, face);
                lightFrustum.Extract(vp);
                shadowQueue.Build(lightFrustum, l.Position);

                PointShadows.BeginFace(slot, face);
                Shader shader = BindDepthMaterial(_mat_PointShadowDepth, false); /// RH matrices flip winding, so no culling
                shader.SetMatrix4x4("uFaceViewProj", vp);
                shader.SetVector3("uLightPos", l.Position);
                shader.SetFloat("uLightRange", l.Range);
                DrawDepthQueue(shadowQueue);
            }
        }
        PointShadows.End();
    }

    Shader BindDepthMaterial (Material material, bool cullBack) {
        Shader shader = state.Bind(material);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthMask(true);
        GL.ColorMask(false, false, false, false);
        if (cullBack) {
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(TriangleFace.Back);
        } else {
            GL.Disable(EnableCap.CullFace);
        }
        return shader;
    }

    void DrawDepthQueue (RenderQueue q) {
        int count = q.Count;
        int idx = 0;
        while (idx < count) {
            RenderData first = q[idx];
            int runEnd = q.RunEnd(idx);
            if (first.material.Pass == RenderPass.Opaque) DrawDepthRun(q, idx, runEnd);
            idx = runEnd;
        }
    }

    /// The depth material is already bound by the caller, so a run is only the instanced draw
    void DrawDepthRun (RenderQueue q, int startIndex, int endIndexExclusive) {
        int runLength = endIndexExclusive - startIndex;
        if (instanceModelScratch.Length < runLength) {
            instanceModelScratch = new Matrix4x4[runLength];
        }
        RenderData first = q[startIndex];
        for (int i = 0; i < runLength; i++) {
            instanceModelScratch[i] = q[startIndex + i].model;
        }
        first.mesh.DrawInstanced(new ReadOnlySpan<Matrix4x4>(instanceModelScratch, 0, runLength),
            first.indexOffset, first.indexCount, first.primitiveType);
    }




    protected void OnFrameBufferResize (Silk.NET.Maths.Vector2D<int> newSize) {
        gl.Viewport(newSize);
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