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

        _visibleIndexComparer = new RenderInfoIndexComparer(RenderList);

        Engine.Instance.de_Render += Render;
        Windows.Window.FramebufferResize += OnFrameBufferResize;
        Windows.Window.Closing += Dispose;

        _GL = Windows.Window.CreateOpenGL();
        GLDebug.Init();
        GL.FrontFace(FrontFaceDirection.CW);
        GL.ClearColor(Constants.clearColor.X, Constants.clearColor.Y, Constants.clearColor.Z, 1f);

        Skybox = new Skybox(_hdr_Skybox);

        //SetTargetSize(Engine.Window.Size.X, Engine.Window.Size.Y);

        Stats = new RendererStats();

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

    public Camera? Camera = null!;

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
    protected readonly List<int> _visibleIndices = new List<int>();
    protected readonly Vector4[] _frustumPlanes = new Vector4[4]; /// left, right, bottom, top — (normal.xyz, d); inside test is dot(normal, p) + d >= 0

    protected Matrix4x4[] _instanceModelScratch = Array.Empty<Matrix4x4>();
    protected Matrix4x4[] _instanceNormalScratch = Array.Empty<Matrix4x4>();

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



    public virtual void Render () {
        /// Clear Fraame
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

        /// Render
        Camera = MainCamera;
        if (Camera is null) {
            Log.log($"No {nameof(Graphics.Camera)} found");
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
    public void RenderReset () {

    }
    protected void StatsStart () {
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
    protected void StatsEnd () {
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
        ExtractFrustumPlanes(m4x4_View*m4x4_Projection);

        _visibleIndices.Clear();
        int total = RenderList.Count;
        for (int i = 0; i < total; i++) {
            RenderInfo info = RenderList[i];
            if (info.mesh is null || info.material is null) continue;

            if (info.material.pass == RenderPass.UI) {
                _visibleIndices.Add(i); /// UI is screen-space — a world-space frustum test doesn't apply
                continue;
            }

            AABB worldAABB = info.mesh.LocalAABB.Transformed(info.model);
            if (IsInFrustum(worldAABB, _frustumPlanes)) _visibleIndices.Add(i);
        }

        /// Sorting int indices (4 bytes) instead of RenderInfo values directly — RenderInfo
        /// carries a Matrix4x4 plus a Matrix4x4? (~150 bytes total), and Sort() does O(n log n)
        /// swaps of whatever type you give it. Swapping indices instead of full structs cuts
        /// that memory traffic drastically once the cube count gets large.
        _visibleIndices.Sort(_visibleIndexComparer);

        /// Consecutive runs of the same (mesh, material) — guaranteed adjacent by the sort's
        /// mesh/material tie-break above — get drawn as one instanced call instead of one
        /// draw call each. Every shader used here must read the model/normal matrix from the
        /// instanced attributes (locations 3 and 7) — see DrawInstancedRun / Mesh.DrawInstanced.
        int count = _visibleIndices.Count;
        int idx = 0;
        while (idx < count) {
            RenderInfo first = RenderList[_visibleIndices[idx]];

            if (first.material.pass == RenderPass.UI) {
                DrawRenderInfo(first);
                idx++;
                continue;
            }

            int runEnd = idx + 1;
            while (runEnd < count) {
                RenderInfo next = RenderList[_visibleIndices[runEnd]];
                if (!ReferenceEquals(next.mesh, first.mesh) || !ReferenceEquals(next.material, first.material)) break;
                runEnd++;
            }

            /// Always instanced — no uniform-based fallback. A run of 1 just becomes a batch
            /// of 1 through the same instanced path; every shader used here MUST read the
            /// model/normal matrix from the instanced attributes, with no uModel/uNormalMatrix
            /// uniform variant to fall back to.
            DrawInstancedRun(idx, runEnd);

            idx = runEnd;
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
        if (Renderer.Instance.Camera is null) return 0;

        int passCompare = a.material.pass.CompareTo(b.material.pass);
        if (passCompare != 0) return passCompare;

        if (a.material.pass == RenderPass.Transparent) {
            float distA = Vector3.DistanceSquared(Renderer.Instance.Camera.CameraPos, a.model.Translation);
            float distB = Vector3.DistanceSquared(Renderer.Instance.Camera.CameraPos, b.model.Translation);
            return distB.CompareTo(distA);
        }

        int shaderCompare = a.material.shader.GetHashCode().CompareTo(b.material.shader.GetHashCode());
        if (shaderCompare != 0) return shaderCompare;

        int materialCompare = a.material.GetHashCode().CompareTo(b.material.GetHashCode());
        if (materialCompare != 0) return materialCompare;

        return a.mesh.GetHashCode().CompareTo(b.mesh.GetHashCode()); /// groups instancing candidates together
    }

    /// A cached IComparer over indices into RenderList — see the comment at the Sort() call
    /// for why we compare indices instead of RenderInfo values directly.
    protected readonly IComparer<int> _visibleIndexComparer;
    protected sealed class RenderInfoIndexComparer : IComparer<int> {
        private readonly List<RenderInfo> _renderList;
        public RenderInfoIndexComparer (List<RenderInfo> renderList) { _renderList = renderList; }
        public int Compare (int a, int b) => CompareRenderInfo(_renderList[a], _renderList[b]);
    }

    /// Shared between DrawRenderInfo and DrawInstancedRun — the pass/cull/depth GL state only
    /// depends on the material, so both paths apply it the same way.
    protected void ApplyMaterialState (Material material) {
        /// Pass
        switch (material.pass) {
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
        switch (material.face) {
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
        if (material.depthTest) GL.Enable(EnableCap.DepthTest);
        else GL.Disable(EnableCap.DepthTest);
        GL.DepthMask(material.depthWrite);
    }

    public void DrawRenderInfo (RenderInfo info) {
        if (Renderer.Instance.Camera is null) return;
        if (info.mesh is null) return;
        if (info.material is null) return;

        bool materialChanged = !ReferenceEquals(info.material, _lastDrawnMaterial);
        if (materialChanged) ApplyMaterialState(info.material);

        Shader shader = info.material.shader;
        if (!ReferenceEquals(shader, _lastDrawnShader)) shader.Use();

        /// Uniforms
        switch (info.material.pass) {
            case RenderPass.Opaque:
            case RenderPass.Transparent:
                SetSceneUniformsUnlit(shader, Renderer.Instance.Camera.CameraPos);
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

    /// Draws a run of identical (mesh, material) entries as one instanced call. Material/shader
    /// state and the shared uniforms (view/projection/lighting/skybox) are applied once for the
    /// whole run instead of once per object — the model/normal matrices go through the instanced
    /// vertex attributes on Mesh instead of the uModel/uNormalMatrix uniforms.
    protected void DrawInstancedRun (int startIndex, int endIndexExclusive) {
        int runLength = endIndexExclusive - startIndex;
        if (_instanceModelScratch.Length < runLength) {
            _instanceModelScratch = new Matrix4x4[runLength];
            _instanceNormalScratch = new Matrix4x4[runLength];
        }

        RenderInfo first = RenderList[_visibleIndices[startIndex]];

        for (int i = 0; i < runLength; i++) {
            RenderInfo info = RenderList[_visibleIndices[startIndex + i]];
            _instanceModelScratch[i] = info.model;
            _instanceNormalScratch[i] = info.normal ?? GetNormalMatrix(info.model);
        }

        ApplyMaterialState(first.material);

        Shader shader = first.material.shader;
        if (!ReferenceEquals(shader, _lastDrawnShader)) shader.Use();

        switch (first.material.pass) {
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

        first.material.Apply();

        first.mesh.DrawInstanced(
            new ReadOnlySpan<Matrix4x4>(_instanceModelScratch, 0, runLength),
            new ReadOnlySpan<Matrix4x4>(_instanceNormalScratch, 0, runLength),
            first.primitiveType);

        _lastDrawnMaterial = first.material;
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