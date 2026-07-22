using Silk.NET.OpenGL;
using Engine.Graphics.UI;
using static Engine.Graphics.Shader;

namespace Engine.Graphics;


public class Renderer {
    public Renderer () {
        //Log.log($"Renderer ctor called, hash {GetHashCode()}");
        if (Instance is not null) Instance.Dispose();
        Instance = this;

        Engine.Window.Render += OnRender;
        Engine.Window.Closing += Dispose;
        Engine.Window.FramebufferResize += OnFrameBufferResize;

        GL = Engine.Window.CreateOpenGL();
        GLDebug.Init();
        GL.FrontFace(FrontFaceDirection.CW);
        GL.ClearColor(0.1f, 0.1f, 0.15f, 1f);

        TextRenderer = new TextRenderer();

        /// Delegates
        de_GizmosUpdate += AssetsEngine._gizmo_Selected.Update;
        de_GizmosDraw += AssetsEngine._gizmo_Selected.Draw;

        Engine.Instance.de_Update += de_GizmosUpdate;
    }
    public static Renderer Instance = null!;

    public readonly GL GL = null!;
    public readonly TextRenderer TextRenderer = null!;

    public Action? de_GizmosUpdate = null;
    public Action? de_GizmosDraw = null;
    public Action? de_Dispose = null;

    public readonly static Matrix4x4 _modelIdentity = Matrix4x4.Identity;
    public readonly static float[] _uModelIdentity = Matrix4x4.ToArray(_modelIdentity);
    
    /// Debug
    internal Matrix4x4 m4x4_View = Matrix4x4.Identity;
    internal Matrix4x4 m4x4Projection = Matrix4x4.Identity;
    private float[] uView = [];
    public float[] UView => uView;

    private float[] uProjection = [];
    public float[] UProjection => uProjection;


    private readonly List<RenderInfo> RenderList = new List<RenderInfo>();
    public void AddRenderInfo (RenderInfo renderInfo) {
        RenderList.Add(renderInfo);
    }



    public static void SetSceneUniformsLit (Shader shader) {
        Instance.SetSceneUniformsUnlit(shader);
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
    public void SetSceneUniformsUnlit (Shader shader) {
        shader.Use();
        shader.SetMatrix4(View, uView);
        shader.SetMatrix4(Projection, uProjection);
        shader.SetVector3(ViewPos, Camera.Instance.cameraPos);
    }

    private void Draw () {
        int count = RenderList.Count;
        for (int i = 0; i < RenderList.Count; i++) {
            DrawMesh(RenderList[i]);
        }
    }
    internal void DrawMesh (RenderInfo renderInfo) {
        if (renderInfo.mesh is null) return;
        if (renderInfo.material is null) return;
        if (renderInfo.shader is null) return;

        Matrix4x4 mesh_m4x4 = Matrix4x4.CreateScale(renderInfo.scale) 
            *Matrix4x4.RotationEuler(renderInfo.rot)*Matrix4x4.Position(renderInfo.pos);
        float[] mesh_uModel = Matrix4x4.ToArray(mesh_m4x4);

        SetSceneUniformsLit(renderInfo.shader);
        renderInfo.shader.SetMatrix4(Model, mesh_uModel);
        renderInfo.material.Apply(renderInfo.shader);
        renderInfo.mesh.Draw(renderInfo.primitiveType);
    }
    

    private void OnRender (double deltaTime) {
        UpdateProjection();

        GL.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit));
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);

        if (Constants.renderSkybox) AssetsEngine._skybox?.Draw(m4x4_View, m4x4Projection);

        GL.CullFace(TriangleFace.Back);

        /// Draw Scene
        Draw();

        //DrawSelectedOutline();

        SceneManager.ActiveScene?.DrawRaw();

        ///--- Stage Post-Scene ---///
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        /// Draw Gizmos
        if (Constants.drawGizmos) Gizmos. DrawGizmos();

        /// Draw UI
        TextRenderer.DrawUI();

        EditorUI.Instance.Draw();


        DrawEnd();
    }
    private void UpdateProjection () {
        float aspect = Engine.Window.Size.X/(float)Engine.Window.Size.Y;
        m4x4Projection = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(Constants._cameraFOV, aspect, Constants._cameraPlaneClose, Constants._cameraPlaneFar);
        uView = Matrix4x4.ToArray(m4x4_View);
        uProjection = Matrix4x4.ToArray(m4x4Projection);
    }

    

    private void DrawEnd () {
        RenderList.Clear();
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
