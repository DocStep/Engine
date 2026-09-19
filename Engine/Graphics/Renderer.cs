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

    protected readonly List<RenderInfo> RenderList = new List<RenderInfo>();
    protected readonly List<RenderInfo> _visibleList = new List<RenderInfo>();
    protected readonly Vector4[] _frustumPlanes = new Vector4[4]; /// left, right, bottom, top — (normal.xyz, d); inside test is dot(normal, p) + d >= 0

    public RendererStats Stats = new RendererStats(); /// set in ctor
    public int Width => (int)MathF.Round(Stats.SceneSize.X);
    public int Height => (int)MathF.Round(Stats.SceneSize.Y);

    protected System.Diagnostics.Stopwatch sw_Latency = new System.Diagnostics.Stopwatch();

    /// Cached so UpdateProjection only rebuilds the UI ortho matrix when size actually changes
    protected float _lastProjWidth = -1f;
    protected float _lastProjHeight = -1f;

    /// State-change dedup across a frame's draw calls — reset each frame in DrawStart
    protected Material? _lastDrawnMaterial = null;
    protected Shader? _lastDrawnShader = null;



    public void Render () {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

        if (Camera.Main is null) {
            Log.log($"No {nameof(Camera)} found");
            return;
        }
        DrawStart();

        de_PreRender?.Invoke();

        SetTargetSize();

        PostProcess.Resize(Width, Height);

        /// Camera Matrix
        m4x4_View = Camera.Main.GetViewMatrix();
        UpdateProjection(Width, Height);

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

        DrawEnd();

        //Log.log("Renderer", Stats.Frame, Windows.Window.Size, Stats.SceneSize);
        //Thread.Sleep(500);
    }
    public void RenderReset () {

    }
    protected void DrawStart () {
        sw_Latency.Restart();

        Stats.DrawCalls = 0;
        Stats.PostProccessCalls = 0;
        Stats.DrawCallsUI = 0;
        Stats.WindowSize = new Vector2(Windows.Window.Size.X, Windows.Window.Size.Y);
        Stats.SceneSize = Stats.WindowSize;

        /// GL state from a previous frame (post-process, gizmos, editor UI) may not match
        /// the first material we draw — force the first DrawRenderInfo call to re-apply state
        _lastDrawnMaterial = null;
        _lastDrawnShader = null;
    }
    protected void DrawEnd () {
        Stats.Latency = (float)sw_Latency.Elapsed.TotalMilliseconds;

        RenderList.Clear();
    }
    public virtual void SetTargetSize () {
        Stats.SceneSize = new Vector2(Windows.Window.Size.X, Windows.Window.Size.Y);
    }
    public virtual void PresentToBackbuffer () {
        PostProcess.PresentToBackbuffer(); /// blit _outputFbo into fbo 0
    }

    public void AddRenderInfo (RenderInfo renderInfo) {
        RenderList.Add(renderInfo);
    }

    protected void UpdateProjection (float width, float height) {
        float aspect = width/height;
        m4x4_Projection = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(
            Camera.Main.FOV*Mathf.Deg2Rad, aspect, Camera.Main.planeNear, Camera.Main.planeFar);

        /// Ortho only depends on width/height, not the camera — skip rebuilding it every frame
        if (width != _lastProjWidth || height != _lastProjHeight) {
            m4x4_ProjectionUI = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, -1f, 1f);
            _lastProjWidth = width;
            _lastProjHeight = height;
        }
    }

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
        ExtractFrustumPlanes(m4x4_View*m4x4_Projection);

        _visibleList.Clear();
        int total = RenderList.Count;
        for (int i = 0; i < total; i++) {
            RenderInfo info = RenderList[i];
            if (info.mesh is null || info.material is null) continue;

            if (info.material.pass == RenderPass.UI) {
                _visibleList.Add(info); /// UI is screen-space — a world-space frustum test doesn't apply
                continue;
            }

            AABB worldAABB = info.mesh.LocalAABB.Transformed(info.model);
            if (IsInFrustum(worldAABB, _frustumPlanes)) _visibleList.Add(info);
        }

        _visibleList.Sort(_renderInfoComparer);
        int count = _visibleList.Count;
        for (int i = 0; i < count; i++) {
            RenderInfo info = _visibleList[i];
            //if (info.mesh.Name == "SuzanneHighRes") s++;
            DrawRenderInfo(info);
        }
        //Log.log("SuzanneHighRes", s);
    }

    /// Row-vector convention (v*M, matching the ScreenPointToRay code): a clip-space
    /// component is a dot product of v with a COLUMN of M, not a row — the opposite of the
    /// textbook (column-vector) Gribb-Hartmann derivation. Left/right/bottom/top are
    /// convention-independent either way. Near/far are deliberately not tested here — they
    /// depend on whether the projection's depth range is [-1,1] or [0,1], which I can't
    /// confirm from this file alone, and getting that wrong silently pops objects in and out
    /// near the camera. Side-plane culling still catches the common "off to the side" case.
    protected void ExtractFrustumPlanes (Matrix4x4 viewProj) {
        _frustumPlanes[0] = new Vector4(viewProj.M14+viewProj.M11, viewProj.M24+viewProj.M21, viewProj.M34+viewProj.M31, viewProj.M44+viewProj.M41); /// Left
        _frustumPlanes[1] = new Vector4(viewProj.M14-viewProj.M11, viewProj.M24-viewProj.M21, viewProj.M34-viewProj.M31, viewProj.M44-viewProj.M41); /// Right
        _frustumPlanes[2] = new Vector4(viewProj.M14+viewProj.M12, viewProj.M24+viewProj.M22, viewProj.M34+viewProj.M32, viewProj.M44+viewProj.M42); /// Bottom
        _frustumPlanes[3] = new Vector4(viewProj.M14-viewProj.M12, viewProj.M24-viewProj.M22, viewProj.M34-viewProj.M32, viewProj.M44-viewProj.M42); /// Top
    }

    /// Positive-vertex AABB/plane test: for each plane, only the corner furthest along the
    /// plane's normal can fail the test, so we pick that corner directly instead of testing all 8.
    protected static bool IsInFrustum (AABB worldAABB, Vector4[] planes) {
        for (int i = 0; i < planes.Length; i++) {
            Vector4 plane = planes[i];

            float px = 0f <= plane.X ? worldAABB.Max.X : worldAABB.Min.X;
            float py = 0f <= plane.Y ? worldAABB.Max.Y : worldAABB.Min.Y;
            float pz = 0f <= plane.Z ? worldAABB.Max.Z : worldAABB.Min.Z;

            if (plane.X*px + plane.Y*py + plane.Z*pz + plane.W < 0f) return false;
        }
        return true;
    }

    /// Sort order: pass first (Opaque < Transparent < UI), then:
    /// - Transparent: back-to-front by camera distance — required for correct blending,
    ///   not just an optimization; without this overlapping transparent surfaces blend wrong.
    /// - Opaque/UI: by shader then material, so DrawRenderInfo can skip redundant GL state
    ///   changes between consecutive draws. (Front-to-back early-Z sorting would fight this —
    ///   pick that instead of material batching if overdraw turns out to be the bigger cost.)
    protected static int CompareRenderInfo (RenderInfo a, RenderInfo b) {
        if (Camera.Main is null) return 0;

        int passCompare = a.material.pass.CompareTo(b.material.pass);
        if (passCompare != 0) return passCompare;

        if (a.material.pass == RenderPass.Transparent) {
            float distA = Vector3.DistanceSquared(Camera.Main.CameraPos, a.model.Translation);
            float distB = Vector3.DistanceSquared(Camera.Main.CameraPos, b.model.Translation);
            return distB.CompareTo(distA);
        }

        int shaderCompare = a.material.shader.GetHashCode().CompareTo(b.material.shader.GetHashCode());
        if (shaderCompare != 0) return shaderCompare;

        return a.material.GetHashCode().CompareTo(b.material.GetHashCode());
    }

    /// A cached IComparer instance instead of passing CompareRenderInfo as Comparison<T>
    /// directly to Sort() — some BCL versions wrap a Comparison<T> in a throwaway comparer
    /// object internally on every call. This is guaranteed zero allocation regardless.
    protected static readonly IComparer<RenderInfo> _renderInfoComparer = new RenderInfoComparer();
    protected sealed class RenderInfoComparer : IComparer<RenderInfo> {
        public int Compare (RenderInfo a, RenderInfo b) => CompareRenderInfo(a, b);
    }

    public void DrawRenderInfo (RenderInfo info) {
        if (Camera.Main is null) return;
        if (info.mesh is null) return;
        if (info.material is null) return;

        bool materialChanged = !ReferenceEquals(info.material, _lastDrawnMaterial);

        if (materialChanged) {
            /// Pass
            switch (info.material.pass) {
                case RenderPass.Opaque:
                    GL.Disable(EnableCap.Blend);
                    break;
                case RenderPass.Transparent:
                case RenderPass.UI:
                    GL.Enable(EnableCap.Blend);
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                    break;
            }

            /// CullFace
            switch (info.material.face) {
                case RenderFace.Front:
                    GL.Enable(EnableCap.CullFace);
                    GL.CullFace(TriangleFace.Back);
                    break;
                case RenderFace.Back:
                    GL.Enable(EnableCap.CullFace);
                    GL.CullFace(TriangleFace.Front);
                    break;
                case RenderFace.Both:
                    GL.Disable(EnableCap.CullFace);
                    break;
            }

            /// Depth
            if (info.material.depthTest) GL.Enable(EnableCap.DepthTest);
            else GL.Disable(EnableCap.DepthTest);
            GL.DepthMask(info.material.depthWrite);
        }

        Shader shader = info.material.shader;
        if (!ReferenceEquals(shader, _lastDrawnShader)) shader.Use();

        /// Uniforms
        switch (info.material.pass) {
            case RenderPass.Opaque:
            case RenderPass.Transparent:
                SetSceneUniformsUnlit(shader, Camera.Main.CameraPos);
                SetSceneUniformsLit(shader);
                SetSceneUniformsSkybox(shader, Skybox.texture, Skybox.maxLod);
                break;
            case RenderPass.UI:
                shader.SetMatrix4x4(Projection, m4x4_ProjectionUI);
                break;
        }

        shader.SetMatrix4x4(Model, info.model);
        shader.SetMatrix4x4(NormalMatrix, info.normal ?? GetNormalMatrix(info.model));

        info.material.Apply();

        info.mesh.Draw(info.primitiveType);

        _lastDrawnMaterial = info.material;
        _lastDrawnShader = shader;
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




    protected List<RenderInfo> RenderListSame = new List<RenderInfo>();
    protected void DrawSame () {
        if (Stats.Frame == 0) {
            RenderListSame.AddRange(RenderList);
        } else {
            RenderList.Clear();
            RenderList.AddRange(RenderListSame);
        }
        //if (iter == 100) Thread.Sleep(10000);
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