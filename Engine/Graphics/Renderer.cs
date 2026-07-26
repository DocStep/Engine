using Silk.NET.OpenGL;
using Engine.Graphics.UI;
using static Engine.Graphics.Shader;
using static Engine.AssetsEngine;
using System.Threading;

namespace Engine.Graphics;


public class Renderer {
    public Renderer () {
        //Log.log($"Renderer ctor called, hash {GetHashCode()}");
        if (Instance is not null) Instance.Dispose();
        Instance = this;

        Engine.Window.Render += OnRender;
        Engine.Window.FramebufferResize += OnFrameBufferResize;
        Engine.Window.Closing += Dispose;

        _GL = Engine.Window.CreateOpenGL();
        GLDebug.Init();
        GL.FrontFace(FrontFaceDirection.CW);
        GL.ClearColor(0.1f, 0.1f, 0.15f, 1f);

        _skybox = new Skybox(_sh_Skybox, _hdrTexture_Skybox) {
            BlurScale = 3f
        };

        _postProcessStack = new PostProcessStack();
        _postProcessStack.Effects.Add(new GrayscaleEffect(AssetsEngine._sh_Grayscale));

        TextRenderer = new TextRenderer();

        /// Delegates
        //de_GizmosUpdate += Gizmos._gizmo_Selected.Update;
        //de_GizmosDraw += Gizmos._gizmo_Selected.Draw;

        //Engine.Instance.de_Update += de_GizmosUpdate;
    }
    public static Renderer Instance = null!;

    private readonly GL _GL = Engine.Window.CreateOpenGL();
    public static GL GL => Instance._GL;

    public readonly TextRenderer TextRenderer = null!;

    //public Action? de_GizmosUpdate = null;
    //public Action? de_GizmosDraw = null;
    public Action? de_Dispose = null;

    //public readonly static Matrix4x4 _modelIdentity = Matrix4x4.Identity;
    //public readonly static float[] _uModelIdentity = Matrix4x4.ToArray(Matrix4x4.Identity);

    public readonly Skybox _skybox = null!;

    public readonly PostProcessStack _postProcessStack = null!;


    /// Debug
    internal Matrix4x4 m4x4_View = Matrix4x4.Identity;
    internal Matrix4x4 m4x4Projection = Matrix4x4.Identity;
    private static float[] uView = [];
    public float[] UView => uView;

    private static float[] uProjection = [];
    public float[] UProjection => uProjection;

    public RendererStats Stats = default;


    private readonly List<RenderInfo> RenderList = new List<RenderInfo>();
    private List<RenderInfo> RenderListStatic = new List<RenderInfo>();
    public void AddRenderInfo (RenderInfo renderInfo) {
        RenderList.Add(renderInfo);
    }
    long iter = 0;


    private void OnRender (double deltaTime) {
        try {
            Stats = new RendererStats();
            Gizmos.Update();
            Gizmos._gizmo_Selected.Update();

            UpdateProjection();
            _postProcessStack.BeginScene();

            _skybox?.Draw(m4x4_View, m4x4Projection);

            DrawMaterialsGrid(-14f, 0f);

            DrawMeshes();
            SceneManager.ActiveScene?.DrawRaw();

            bool postProcessingEnabled = SettingsGraphicsEngine.Instance?.postProcessing.isOn ?? true;
            _postProcessStack.EndSceneAndRunStack(0, postProcessingEnabled);

            Gizmos._gizmo_Selected.Draw();
            Gizmos.Draw();
            TextRenderer.Draw();
            EditorUI.Instance.Draw();

            iter++;
            DrawEnd();
        } catch (Exception ex) {
            Log.log($"OnRender exception: {ex}");
        }
    }
    private void DrawEnd () {
        RenderList.Clear();
    }


    //private static RenderPass currentPass = RenderPass.undefined;
    private void DrawMeshes () {
        //DrawSame();
        RenderList.Sort((a, b) => a.material.pass.CompareTo(b.material.pass));
        int count = RenderList.Count;
        for (int i = 0; i < count; i++) {
            RenderInfo info = RenderList[i];
            DrawMesh(info);
        }
    }

    public void DrawMesh (RenderInfo info) {
        if (info.mesh is null) return;
        if (info.material is null) return;

        Shader shader = info.material.shader;

        switch (info.material.pass) {
            case RenderPass.Opaque:
                GL.Disable(EnableCap.Blend);
                break;
            case RenderPass.Transparent:
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                break;
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

        if (info.material.depthTest) {
            GL.Enable(EnableCap.DepthTest);
        } else GL.Disable(EnableCap.DepthTest);

        GL.DepthMask(info.material.depthWrite);

        if (info.depthRangeNear != 0 || info.depthRangeFar != 1) 
            GL.DepthRange(info.depthRangeNear, info.depthRangeFar);

        SetSceneUniformsUnlit(shader);
        SetSceneUniformsLit(shader);
        SetSceneUniformsSkybox(shader, _skybox.texture, _skybox.maxLod);

        Matrix4x4 mesh_m4x4 = info.modelOverride ?? Matrix4x4.CreateScale(info.scale) 
            *Matrix4x4.RotationEuler(info.rot)*Matrix4x4.Position(info.pos);
        float[] mesh_uModel = Matrix4x4.ToArray(mesh_m4x4);
        shader.SetMatrix4(Model, mesh_uModel);
        info.material.Apply(shader);

        info.de_Pre?.Invoke();
        info.mesh.Draw(info.primitiveType);
        info.de_Post?.Invoke();

        if (info.depthRangeNear != 0 || info.depthRangeFar != 1) 
            GL.DepthRange(0, 1);
    }
    private void UpdateProjection () {
        float aspect = Engine.Window.Size.X/(float)Engine.Window.Size.Y;
        m4x4Projection = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(Constants._cameraFOV, aspect, Constants._cameraPlaneClose, Constants._cameraPlaneFar);
        uView = Matrix4x4.ToArray(m4x4_View);
        uProjection = Matrix4x4.ToArray(m4x4Projection);
    }

    public static void SetSceneUniformsSkybox (Shader shader, HdrTexture? texture, float maxLod) {
        if (texture is null) return;

        texture.Bind(TextureUnit.Texture0);
        shader.SetInt(Shader.Skybox, 0);
        shader.SetFloat(MaxReflectionLod, maxLod);
    }
    public static void SetSceneUniformsLit (Shader shader) {
        shader.SetVector3(SunLightColor, Constants.sunLightColor);
        shader.SetVector3(SunLightDir, Constants.sunLightDir);
        shader.SetVector3(AmbientColor, 0.05f, 0.05f, 0.06f);
        shader.SetFloat(SunLightIntensity, Constants.sunLightIntensity);
        shader.SetFloat(ReflectionIntensity, Constants.reflectionIntensity);
    }
    public static void SetSceneUniformsUnlit (Shader shader) {
        shader.Use();
        shader.SetMatrix4(View, uView);
        shader.SetMatrix4(Projection, uProjection);
        shader.SetVector3(ViewPos, Camera.Instance.cameraPos);
    }




    public static void DrawMaterialsGrid (float offsetX, float offsetZ, int testGridCount = 10, float testGridDensity = 1f) {
        if (!Constants.drawMaterialsGrid) return;

        int total = testGridCount*(int)testGridDensity;
        float speed = 2f;
        for (int x = 0; x < total; x++) {
            for (int z = 0; z < total; z++) {
                float smoothness = (float)x/(total - 1);
                float metallic = (float)z/(total - 1);

                Material mat = new Material(_mat_MaterialPreview);
                mat.SetVector3(Color, Constants.lightGray);
                mat.SetFloat(Smoothness, smoothness);
                mat.SetFloat(Metallic, metallic);

                float _x = x/testGridDensity + offsetX;
                float _z = z/testGridDensity + offsetZ;
                float y = 0.25f*MathF.Sin(_x + speed*(float)Engine.time) * MathF.Cos(_z + speed*(float)Engine.time);
                RenderInfo info = new RenderInfo() {
                    pos = new Vector3(_x, y, _z),
                    mesh = _mesh_Sphere,
                    material = mat,
                };
                Renderer.Instance.AddRenderInfo(info);
            }
        }
    }

    void DrawSame () {
        if (iter == 0) {
            RenderListStatic.AddRange(RenderList);
        } else {
            RenderList.Clear();
            RenderList.AddRange(RenderListStatic);
        }
        //if (iter == 100) Thread.Sleep(10000);
    }

    internal void OnFrameBufferResize (Silk.NET.Maths.Vector2D<int> newSize) {
        GL.Viewport(newSize);
        if (0 < newSize.X && 0 < newSize.Y) {
            UpdateProjection();
            _postProcessStack.Resize(newSize.X, newSize.Y);
        }
    }

    internal void Dispose () {
        TextRenderer.Dispose();

        _skybox.Dispose();
        _postProcessStack.Dispose();

        de_Dispose?.Invoke();
    }

}
