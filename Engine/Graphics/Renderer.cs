using Silk.NET.OpenGL;
using Engine.Graphics.UI;
using static Engine.Graphics.Shader;
using static Engine.AssetsEngine;
using System.Threading;

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
        if (Instance is not null) 
            throw new Exception($"[ctor] {typeof(Renderer)}.{nameof(Instance)} ({GetHashCode()}) is not null");
        
        Instance = this;

        Engine.Instance.de_Render += RenderScene;
        Engine.Window.FramebufferResize += OnFrameBufferResize;
        Engine.Window.Closing += Dispose;

        _GL = Engine.Window.CreateOpenGL();
        GLDebug.Init();
        GL.FrontFace(FrontFaceDirection.CW);
        GL.ClearColor(Constants.clearColor.X, Constants.clearColor.Y, Constants.clearColor.Z, 1f);

        _skybox = new Skybox(_sh_Skybox, _hdr_Skybox) {
            blurScale = 3f,
        };

        //SetTargetSize(Engine.Window.Size.X, Engine.Window.Size.Y);

        PostProcess = new PostProcessStack();
        
        TextRenderer = new TextRenderer();

        /// Delegates
        de_DrawUI += TextRenderer.Draw;


        //PostProcess.Effects.Add(new PostProcessPass(_mat_Fullscreen));
        //PostProcess.Effects.Add(new PostProcessPass(_mat_Depth));
        //PostProcess.Effects.Add(new PostProcessPass(_mat_Grayscale));
        PostProcess.Effects.Add(new PostProcessPass(_mat_SSAO));
        PostProcess.Effects.Add(new PostProcessPass(_mat_SSAOBlur));
        PostProcess.Effects.Add(new PostProcessPass(_mat_SSAOComposite));
        //PostProcess.Effects.Add(new PostProcessPass(_mat_CameraFocus));
        PostProcess.Effects.Add(new PostProcessPass(_mat_Fxaa));
    }

    public static Renderer Instance = null!;

    protected readonly GL _GL = Engine.Window.CreateOpenGL();
    public static GL GL => Instance._GL;

    public readonly TextRenderer TextRenderer = null!;

    public Action? de_Dispose = null;

    public readonly Skybox _skybox = null!;

    public readonly PostProcessStack PostProcess = null!;


    public int Width { get; protected set; }
    public int Height { get; protected set; }


    /// Debug
    public Matrix4x4 m4x4_View = Matrix4x4.Identity;
    public Matrix4x4 m4x4Projection = Matrix4x4.Identity;
    protected static float[] uView = [];
    public float[] UView => uView;

    protected static float[] uProjection = [];
    public float[] UProjection => uProjection;

    //protected RendererStats stats = new RendererStats();
    public RendererStats Stats = new RendererStats();
    protected System.Diagnostics.Stopwatch sw_Latency = new System.Diagnostics.Stopwatch();

    protected readonly List<RenderInfo> RenderList = new List<RenderInfo>();
    protected readonly List<RenderInfo> RenderGizmoList = new List<RenderInfo>();
    protected readonly List<RenderInfo> RenderUIList = new List<RenderInfo>();
    public void AddRenderInfo (RenderInfo renderInfo) {
        switch (renderInfo.material.pass) {
            case RenderPass.Opaque:
                RenderList.Add(renderInfo);
                break;
            case RenderPass.Transparent:
            case RenderPass.Gizmo:
                RenderGizmoList.Add(renderInfo);
                break;
            case RenderPass.UI:
                RenderUIList.Add(renderInfo);
                break;
        }
    }
    protected long iter = 0;


    public Action? de_LateUpdate = null;
    public Action? de_DrawPostScene = null;
    public Action? de_DrawOverlay = null;
    public Action? de_DrawUI = null;
    public Action? de_Final = null;

    public void RenderScene () {
        de_LateUpdate?.Invoke();

        Stats = new RendererStats();
        sw_Latency.Restart();

        SetTargetSize();
        PostProcess.Resize(Width, Height);

        /// Camera Matrix
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

        if (Camera.Current is null) {
            Log.log($"No {nameof(Camera)} found");
            return;
        }

        UpdateProjection(Width, Height);

        PostProcess.BeginScene();

        _skybox?.Draw(m4x4_View, m4x4Projection);

        DrawSceneAll();

        SceneManager.ActiveScene?.DrawRaw();

        PostProcess.Run();

        de_DrawPostScene?.Invoke();

        PostProcess.BindOutputForOverlay();

        de_DrawOverlay?.Invoke();

        /// UI Stage
        de_DrawUI?.Invoke();

        PresentToBackbuffer();

        iter++;
        DrawEnd();
    }
    public virtual void SetTargetSize () {
        Width = Engine.Window.Size.X;
        Height = Engine.Window.Size.Y;
    }
    public virtual void PresentToBackbuffer () {
        PostProcess.PresentToBackbuffer(); /// blit _outputFbo into fbo 0
    }
    protected void DrawEnd () {
        Stats.Latency = (float)sw_Latency.Elapsed.TotalMilliseconds;
        Stats.Frame = iter;

        RenderList.Clear();
        RenderGizmoList.Clear();
        RenderUIList.Clear();
    }


    protected void UpdateProjection (float width, float height) {
        float aspect = width/height;
        m4x4Projection = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(
            Camera.Current.FOV/180*MathF.PI, aspect, Camera.Current.planeNear, Camera.Current.planeFar);
        uView = Matrix4x4.ToArray(m4x4_View);
        uProjection = Matrix4x4.ToArray(m4x4Projection);
    }

    protected virtual void DrawSceneAll () {
        switch (Constants.drawMode) {
            case DrawMode.Normal:
                DrawScene();
                break;
        }
    }
    protected void DrawScene () {
        //DrawSame();
        RenderList.Sort((a, b) => a.material.pass.CompareTo(b.material.pass));
        int count = RenderList.Count;
        for (int i = 0; i < count; i++) {
            RenderInfo info = RenderList[i];
            DrawInfo(info);
        }
    }

    public void DrawInfo (RenderInfo info) {
        if (info.mesh is null) return;
        if (info.material is null) return;

        Shader shader = info.material.shader;

        switch (info.material.pass) {
            case RenderPass.Opaque:
                GL.Disable(EnableCap.Blend);
                break;
            case RenderPass.Transparent:
            case RenderPass.Gizmo:
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                break;
            case RenderPass.UI:
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                break;
        }

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

        if (info.material.depthTest) GL.Enable(EnableCap.DepthTest);
        else GL.Disable(EnableCap.DepthTest);

        GL.DepthMask(info.material.depthWrite);

        //if (info.depthRangeNear != 0 || info.depthRangeFar != 1)
        //    GL.DepthRange(info.depthRangeNear, info.depthRangeFar);

        SetSceneUniformsUnlit(shader);
        SetSceneUniformsLit(shader);
        SetSceneUniformsSkybox(shader, _skybox.texture, _skybox.maxLod);

        Matrix4x4 mesh_m4x4 = info.modelOverride ?? Matrix4x4.CreateScale(info.scale)
            *Matrix4x4.RotationEuler(info.rot)*Matrix4x4.Position(info.pos);
        float[] mesh_uModel = Matrix4x4.ToArray(mesh_m4x4);
        shader.SetMatrix4(Model, mesh_uModel);
        info.material.Apply();

        info.de_Pre?.Invoke();
        info.mesh.Draw(info.primitiveType);
        info.de_Post?.Invoke();
    }
    

    public static void SetSceneUniformsUnlit (Shader shader) {
        shader.Use();
        shader.SetMatrix4(View, uView);
        shader.SetMatrix4(Projection, uProjection);
        shader.SetVector3(ViewPos, Camera.Current.cameraPos);
    }
    public static void SetSceneUniformsLit (Shader shader) {
        shader.SetVector3(SunLightDir, Constants.sunLightDir);
        shader.SetVector3(SunLightColor, Constants.sunLightColor);
        shader.SetFloat(SunLightIntensity, Constants.sunLightIntensity);
        shader.SetVector3(AmbientColor, Constants.ambientColor);
        shader.SetFloat(AmbientColorIntensity, Constants.ambientColorIntensity);

        if (Constants.renderSkyboxReflection)
            shader.SetFloat(ReflectionIntensity, Constants.reflectionIntensity);
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
        if (iter == 0) {
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
        TextRenderer.Dispose();

        _skybox.Dispose();
        PostProcess.Dispose();

        de_Dispose?.Invoke();
    }

}
