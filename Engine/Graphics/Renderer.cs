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
        if (Instance is not null) 
            throw new Exception($"[ctor] {typeof(Renderer)}.{nameof(Instance)} ({GetHashCode()}) is not null");
        
        Instance = this;

        Engine.Instance.de_Render += Render;
        Windows.Window.FramebufferResize += OnFrameBufferResize;
        Windows.Window.Closing += Dispose;

        _GL = Windows.Window.CreateOpenGL();
        GLDebug.Init();
        GL.FrontFace(FrontFaceDirection.CW);
        GL.ClearColor(Constants.clearColor.X, Constants.clearColor.Y, Constants.clearColor.Z, 1f);

        _skybox = new Skybox(_sh_Skybox, _hdr_Skybox) { blurScale = 3f, };

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


    protected readonly GL _GL = Windows.Window.CreateOpenGL();
    public static GL GL => Instance._GL;

    public readonly TextRenderer TextRenderer = null!;

    public Action? de_Dispose = null;

    public readonly Skybox _skybox = null!;

    public readonly PostProcessStack PostProcess = null!;


    /// Debug
    public Matrix4x4 m4x4_View = Matrix4x4.Identity;
    public Matrix4x4 m4x4_Projection = Matrix4x4.Identity;
    public Matrix4x4 m4x4_ProjectionUI = Matrix4x4.Identity;
    //protected static float[] uView = [];
    //public float[] UView => uView;

    //protected static float[] uProjection = [];
    //public float[] UProjection => uProjection;

    protected readonly List<RenderInfo> RenderList = new List<RenderInfo>();

    public RendererStats Stats = new RendererStats();
    public int Width => (int)Stats.SceneSize.X;
    public int Height => (int)Stats.SceneSize.Y;

    protected System.Diagnostics.Stopwatch sw_Latency = new System.Diagnostics.Stopwatch();



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

        _skybox?.Draw(m4x4_View, m4x4_Projection);

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
        m4x4_ProjectionUI = Matrix4x4.CreateOrthographicOffCenter(0, Width, Height, 0, -1f, 1f);
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
        RenderList.Sort((a, b) => a.material.pass.CompareTo(b.material.pass));
        int count = RenderList.Count;
        for (int i = 0; i < count; i++) {
            RenderInfo info = RenderList[i];
            //if (info.mesh.Name == "SuzanneHighRes") s++;
            DrawRenderInfo(info);
        }
        //Log.log("SuzanneHighRes", s);
    }

    public void DrawRenderInfo (RenderInfo info) {
        if (info.mesh is null) return;
        if (info.material is null) return;

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

        Shader shader = info.material.shader;
        shader.Use();

        /// Uniforms
        switch (info.material.pass) {
            case RenderPass.Opaque:
            case RenderPass.Transparent:
                SetSceneUniformsUnlit(shader, Camera.Main.CameraPos);
                SetSceneUniformsLit(shader);
                SetSceneUniformsSkybox(shader, _skybox.texture, _skybox.maxLod);
                break;
            case RenderPass.UI:
                shader.SetMatrix4x4(Projection, m4x4_ProjectionUI);
                break;
        }

        shader.SetMatrix4x4(Model, info.model);

        if (!Matrix4x4.Invert(info.model, out Matrix4x4 inverseModel)) 
            inverseModel = Matrix4x4.Identity; /// fallback — model scale is degenerate, fix at the source (clamp scale.y)
        Matrix4x4 normalMatrix = Matrix4x4.Transpose(inverseModel);
        shader.SetMatrix4x4(NormalMatrix, normalMatrix);

        info.material.Apply();

        info.mesh.Draw(info.primitiveType);
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
        TextRenderer.Dispose();

        _skybox.Dispose();
        PostProcess.Dispose();

        de_Dispose?.Invoke();
    }

}
