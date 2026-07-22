using Silk.NET.OpenGL;
using Engine.Graphics.UI;
using static Engine.Graphics.Shader;
using static Engine.AssetsEngine;

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

        TextRenderer = new TextRenderer();

        /// Delegates
        de_GizmosUpdate += Gizmos._gizmo_Selected.Update;
        de_GizmosDraw += Gizmos._gizmo_Selected.Draw;

        Engine.Instance.de_Update += de_GizmosUpdate;
    }
    public static Renderer Instance = null!;

    private readonly GL _GL = Engine.Window.CreateOpenGL();
    public static GL GL => Instance._GL;
    public readonly TextRenderer TextRenderer = null!;

    public Action? de_GizmosUpdate = null;
    public Action? de_GizmosDraw = null;
    public Action? de_Dispose = null;

    //public readonly static Matrix4x4 _modelIdentity = Matrix4x4.Identity;
    public readonly static float[] _uModelIdentity = Matrix4x4.ToArray(Matrix4x4.Identity);
    
    /// Debug
    internal Matrix4x4 m4x4_View = Matrix4x4.Identity;
    internal Matrix4x4 m4x4Projection = Matrix4x4.Identity;
    private static float[] uView = [];
    public float[] UView => uView;

    private static float[] uProjection = [];
    public float[] UProjection => uProjection;


    private readonly List<RenderInfo> RenderList = new List<RenderInfo>();
    public void AddRenderInfo (RenderInfo renderInfo) {
        RenderList.Add(renderInfo);
    }



    private void OnRender (double deltaTime) {
        Gizmos.Update();

        GL.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit));

        UpdateProjection();

        if (Constants.renderSkybox) 
            AssetsEngine._skybox?.Draw(m4x4_View, m4x4Projection);

        GL.CullFace(TriangleFace.Back);

        /// Draw Scene
        DrawMaterialsGrid(-12f, 0f, 5, 1f); /// Debug
        Draw();

        SceneManager.ActiveScene?.DrawRaw();

        ///--- Stage Post-Scene ---///
        Gizmos.Draw();

        /// Draw UI
        TextRenderer.Draw();

        EditorUI.Instance.Draw();

        DrawEnd();
    }
    private void DrawEnd () {
        RenderList.Clear();
    }


    private void Draw () {
        RenderList.Sort((a, b) => a.material.pass.CompareTo(b.material.pass));

        int count = RenderList.Count;
        //RenderPass currentPass = RenderPass.undefined;
        for (int i = 0; i < count; i++) {
            RenderInfo info = RenderList[i];
            DrawMesh(info);
        }
    }
    private static void ApplyPassState (Material material) {
        switch (material.pass) {
            case RenderPass.Opaque:
                GL.Enable(EnableCap.CullFace);
                GL.Enable(EnableCap.DepthTest);
                GL.DepthMask(true);
                GL.Disable(EnableCap.Blend);

                SetSceneUniformsLit(material.shader);
                break;
            case RenderPass.Transparent:
                GL.Enable(EnableCap.CullFace);
                GL.Enable(EnableCap.DepthTest);
                GL.DepthMask(true);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                SetSceneUniformsLit(material.shader);
                break;
            case RenderPass.Gizmo:
                GL.Disable(EnableCap.CullFace);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                GL.DepthMask(true);
                break;
            case RenderPass.UI:
                GL.Disable(EnableCap.CullFace);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                //GL.DepthMask(false);
                break;
        }
    }

    public static void DrawMesh (RenderInfo info) {
        if (info.mesh is null) return;
        if (info.material is null) return;

        Shader shader = info.material.shader;
        SetSceneUniformsUnlit(info.material.shader);
        //if (currentPass != info.material.pass) {
        ApplyPassState(info.material);
        //currentPass = info.material.pass;
        //}

        if (info.material.depthTest) GL.Enable(EnableCap.DepthTest);
        else GL.Disable(EnableCap.DepthTest);

        Matrix4x4 mesh_m4x4 = Matrix4x4.CreateScale(info.scale) 
            *Matrix4x4.RotationEuler(info.rot)*Matrix4x4.Position(info.pos);
        float[] mesh_uModel = Matrix4x4.ToArray(mesh_m4x4);

        SetSceneUniformsLit(info.material.shader);
        info.material.shader.SetMatrix4(Model, mesh_uModel);
        info.material.Apply(info.material.shader);
        info.mesh.Draw(info.primitiveType);
    }
    private void UpdateProjection () {
        float aspect = Engine.Window.Size.X/(float)Engine.Window.Size.Y;
        m4x4Projection = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(Constants._cameraFOV, aspect, Constants._cameraPlaneClose, Constants._cameraPlaneFar);
        uView = Matrix4x4.ToArray(m4x4_View);
        uProjection = Matrix4x4.ToArray(m4x4Projection);
    }

    public static void SetSceneUniformsLit (Shader shader) {
        //Instance.SetSceneUniformsUnlit(shader);
        shader.SetVector3(SunLightColor, Constants.sunLightColor);
        shader.SetVector3(SunLightDir, Constants.sunLightDir);
        shader.SetVector3(AmbientColor, 0.05f, 0.05f, 0.06f);
        shader.SetFloat(SunLightIntensity, Constants.sunLightIntensity);
        shader.SetFloat(ReflectionIntensity, Constants.reflectionIntensity);

        if (AssetsEngine._hdrTexture_Skybox is not null) {
            AssetsEngine._hdrTexture_Skybox.Bind(TextureUnit.Texture0);
            shader.SetInt(Shader.Skybox, 0);
            shader.SetFloat(MaxReflectionLod, AssetsEngine.maxLod);
        }
    }
    public static void SetSceneUniformsUnlit (Shader shader) {
        shader.Use();
        shader.SetMatrix4(View, uView);
        shader.SetMatrix4(Projection, uProjection);
        shader.SetVector3(ViewPos, Camera.Instance.cameraPos);
    }




    public static void DrawMaterialsGrid (float offsetX, float offsetZ, int testGridCount, float testGridDensity) {
        Renderer.SetSceneUniformsLit(_sh_Lit);
        _sh_Lit.SetColor(Color, Constants.black);
        for (int x = 0; x < testGridCount*testGridDensity; x++) {
            for (int z = 0; z < testGridCount*testGridDensity; z++) {
                RenderInfo info = new RenderInfo() {
                    pos = new Vector3(2f*x/testGridDensity + offsetX, 0f, 2f*z/testGridDensity + offsetZ),
                    mesh = _mesh_Sphere,
                    material = new Material(_mat_Lit).SetVector3(Color, Constants.lightGray),
                };
                info.material.SetVector3(Color, Constants.lightGray);
                Renderer.Instance.AddRenderInfo(info);
            }
        }
    }



    internal void OnFrameBufferResize (Silk.NET.Maths.Vector2D<int> newSize) {
        GL.Viewport(newSize);
        if (0 < newSize.X && 0 < newSize.Y) 
            UpdateProjection();
    }

    internal void Dispose () {
        TextRenderer.Dispose();

        de_Dispose?.Invoke();
    }

}
