using System.Numerics;
using Silk.NET.OpenGL;
using Engine.Graphics.UI;
using Engine.Input;
using static Engine.DataEngine;

namespace Engine.Graphics;


internal class Renderer {
    public Renderer () {
        Instance = this;

        Engine.Window.Render += OnRender;
        Engine.Window.Closing += OnClosing;
        Engine.Window.FramebufferResize += OnFrameBufferResize;

        GL = Engine.Window.CreateOpenGL();
        GL.ClearColor(0.1f, 0.1f, 0.15f, 1f);
        GL.Enable(EnableCap.DepthTest);

        _mesh_GizmoCube = new Mesh(WireGizmos.Cube(Vector3.Zero, Vector3.One));
        _mesh_GizmoSphere = new Mesh(WireGizmos.Sphere(Vector3.Zero, 0.5f));
        _mesh_GizmoCapsule = new Mesh(WireGizmos.Capsule(-0.5f*Vector3.UnitY, 0.5f*Vector3.UnitY, 0.5f));
        _mesh_GizmoPlane = new Mesh(Plane.GenerateWireframe());
        _mesh_GizmoSun = new Mesh(Arrow.Generate(shaftLength: 1f, shaftRadius: 0.01f, headLength: 0.2f, headRadius: 0.1f));

        _mesh_Cube = new Mesh(Cube.Generate());
        _mesh_Sphere = new Mesh(Sphere.Generate());
        //_mesh_Capsule = new Mesh(Capsule.Generate());
        _mesh_Plane = new Mesh(Plane.Generate());
        _GizmoGrid = new WorldGrid((int)Constants._cameraPlaneFar, 1f);
        _GizmoAxes = new WorldAxes(10f*Constants._cameraPlaneFar);
        _GizmoAxesWidget = new WorldAxes(1f);

        _sh_Lit = new Shader(Utils.LoadSrc("src/Shaders/Lit_Vertex.shader"), Utils.LoadSrc("src/Shaders/Lit_Fragment.shader"), "Lit");
        _sh_Unlit = new Shader(Utils.LoadSrc("src/Shaders/Unlit_Vertex.shader"), Utils.LoadSrc("src/Shaders/Unlit_Fragment.shader"), "Unlit");
        _sh_Grid = new Shader(Utils.LoadSrc("src/Shaders/Grid_Vertex.shader"), Utils.LoadSrc("src/Shaders/Grid_Fragment.shader"), "Grid");
        _sh_Axes = new Shader(Utils.LoadSrc("src/Shaders/Axes_Vertex.shader"), Utils.LoadSrc("src/Shaders/Axes_Fragment.shader"), "Axes");

        _sh_Skybox = new Shader(Utils.LoadSrc("src/Shaders/Skybox_Vertex.shader"), Utils.LoadSrc("src/Shaders/Skybox_Fragment.shader"), "Skybox");
        //_hdrTexture = new HdrTexture("src/hdr/autumn_field_puresky_4k.hdr");
        _hdrTexture_Skybox = new HdrTexture("src/hdr/rogland_clear_night_4k.hdr");
        ///_hdrTexture = new HdrTexture("src/hdr/grasslands_sunset_4k.hdr");
        ///_hdrTexture = new HdrTexture("src/hdr/overcast_soil_puresky_4k.hdr");
        //_hdrTexture = new HdrTexture("src/hdr/qwantani_dusk_2_puresky_4k.hdr");
        _skybox = new Skybox(_sh_Skybox, _hdrTexture_Skybox);
        _skybox.BlurScale = 3f;

        _m_Lit = new Material { Color = Constants.lightGray, };
        _m_Smooth = new Material { Color = Constants.lightGray, Roughness = 0f, };
        _m_Matt = new Material { Color = Constants.lightGray, Roughness = 1f, };
        _m_Metallic = new Material { Color = Constants.gray, Roughness = 0.05f, Metallic = 1, };
        _m_MaterialPreview = new Material { Color = Constants.white, Roughness = 0, Metallic = 1 };

        _m_Unlit = new Material { Color = Constants.gray, Roughness = 1f, };

        _mat_Axes = new Material { Color = Constants.gray, };
        _mat_GizmosG = new Material { Color = Constants.green, Alpha = 0.5f, };

        _m_LitWhite = new Material { Color = Vector3.One, };
        _m_LitBlack = new Material { Color = Vector3.Zero, };
        _m_LitGray = new Material { Color = 0.5f*Vector3.One, };
        _m_LitRed = new Material { Color = new(1, 0, 0), };
        _m_LitGreen = new Material { Color = new(0, 1, 0), };
        _m_LitBlue = new Material { Color = new(0, 0, 1), };

        _mesh_Torus = new Mesh(ObjLoader.Load("src/Models/Torus.obj"));
        _mesh_Suzanne = new Mesh(ObjLoader.Load("src/Models/Suzanne.obj"));
        _mesh_SuzanneHighRes = new Mesh(ObjLoader.Load("src/Models/SuzanneHighRes.obj"));

        _textRenderer = new TextRenderer();
    }

    public static Renderer Instance = null!;

    public readonly TextRenderer _textRenderer = null!;

    public Action? de_DrawGizmos = null;


    internal readonly GL GL = null!;

    public readonly Shader _sh_Lit = null!;
    public readonly Shader _sh_Unlit = null!;
    public readonly Shader _sh_Grid = null!;
    public readonly Shader _sh_Axes = null!;

    public readonly Shader _sh_Skybox = null!;
    public readonly Skybox _skybox = null!;
    public readonly HdrTexture? _hdrTexture_Skybox = null;

    public readonly Material _mat_Axes = null!;
    public readonly Material _mat_GizmosG = null!;

    public readonly Material _m_Lit = null!;
    public readonly Material _m_Unlit = null!;
    public readonly Material _m_Smooth = null!;
    public readonly Material _m_Matt = null!;
    public readonly Material _m_Metallic = null!;
    public readonly Material _m_MaterialPreview = null!;

    public readonly Material _m_LitWhite = null!;
    public readonly Material _m_LitBlack = null!;
    public readonly Material _m_LitGray = null!;
    public readonly Material _m_LitRed = null!;
    public readonly Material _m_LitGreen = null!;
    public readonly Material _m_LitBlue = null!;

    public readonly WorldGrid _GizmoGrid = null!;
    public readonly WorldAxes _GizmoAxes = null!;
    public readonly WorldAxes _GizmoAxesWidget = null!;
    public readonly Mesh _mesh_GizmoSun = null!;

    public readonly Mesh _mesh_GizmoCube = null!;
    public readonly Mesh _mesh_GizmoSphere = null!;
    public readonly Mesh _mesh_GizmoCapsule = null!;
    public readonly Mesh _mesh_GizmoPlane = null!;

    public readonly Mesh _mesh_Cube = null!;
    public readonly Mesh _mesh_Sphere = null!;
    public readonly Mesh _mesh_Capsule = null!;
    public readonly Mesh _mesh_Plane = null!;
    public readonly Mesh _mesh_Torus = null!;
    public readonly Mesh _mesh_Suzanne = null!;
    public readonly Mesh _mesh_SuzanneHighRes = null!;

    private readonly static Matrix4x4 _modelIdentity = Matrix4x4.Identity;
    private readonly static float[] _uModelIdentity = Utils.MatrixToArray(_modelIdentity);
    
    /// Gizmos
    private float cameraOrbitCenterRadius = 0.5f;

    /// Debug
    internal Matrix4x4 View = Matrix4x4.Identity;
    internal Matrix4x4 Projection = Matrix4x4.Identity;
    private float[] uView = [];
    private float[] uProjection = [];

    private bool _renderSkybox = true;
    private bool renderSkybox {
        get => _renderSkybox;
        set {
            if (_renderSkybox != value) {
                _renderSkybox = value;
                renderSkyboxUpdate();
            }
        }
    }
    private void renderSkyboxUpdate () { }
    private bool _renderFPS = true;
    private bool renderFPS {
        get => _renderFPS;
        set {
            if (_renderFPS != value) {
                _renderFPS = value;
            }
        }
    }


    private readonly List<RenderInfo> RenderList = new List<RenderInfo>();
    public void AddRenderInfo (RenderInfo renderInfo) {
        RenderList.Add(renderInfo);
    }



    private void SetSceneUniforms (Shader shader) {
        shader.Use();
        shader.SetMatrix4("uView", uView);
        shader.SetMatrix4("uProjection", uProjection);
        shader.SetVector3("uSunLightColor", Constants.sunLightColor.X, Constants.sunLightColor.Y, Constants.sunLightColor.Z);
        shader.SetVector3("uSunLightDir", Constants.sunLightDir.X, Constants.sunLightDir.Y, Constants.sunLightDir.Z);
        shader.SetVector3("uViewPos", Camera.Instance.cameraPos.X, Camera.Instance.cameraPos.Y, Camera.Instance.cameraPos.Z);
        shader.SetVector3("uAmbientColor", 0.05f, 0.05f, 0.06f);
        shader.SetFloat("uSunLightIntensity", Constants.sunLightIntensity);
        shader.SetFloat("uReflectionIntensity", Constants.reflectionIntensity);
        //shader.SetFloat("uExposure", 1f);

        if (_hdrTexture_Skybox is not null) {
            _hdrTexture_Skybox.Bind(TextureUnit.Texture0);
            shader.SetInt("uSkybox", 0);

            float maxLod = MathF.Log2(MathF.Max(_hdrTexture_Skybox.Width, _hdrTexture_Skybox.Height));
            shader.SetFloat("uMaxReflectionLod", maxLod);
        }
    }


    private void Draw () {
        if (Constants.drawTestGrid) 
            DrawSphereTest(0, -10, Constants.testGridCount, Constants.testGridDensity);

        int count = RenderList.Count;
        for (int i = 0; i < RenderList.Count; i++) {
            if (RenderList[i].mesh is null || RenderList[i].material is null) continue;

            Matrix4x4 mesh_m4x4 = Matrix4x4.CreateScale(RenderList[i].scale)*Matrix4x4.Rotation(RenderList[i].rot)*Matrix4x4.Position(RenderList[i].pos);
            float[] mesh_uModel = Utils.MatrixToArray(mesh_m4x4);

            SetSceneUniforms(RenderList[i].shader);
            RenderList[i].shader.SetMatrix4("uModel", mesh_uModel);
            RenderList[i].material!.Apply(RenderList[i].shader);
            RenderList[i].mesh!.Draw(RenderList[i].primitiveType);
        }
    }
    private void DrawSphereTest (float offsetX, float offsetZ, int testGridCount, float testGridDensity) {
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
        SetSceneUniforms(_sh_Lit);
        _sh_Lit.SetColor("uColor", Constants.black);
        for (int x = 0; x < testGridCount*testGridDensity; x++) {
            for (int z = 0; z < testGridCount*testGridDensity; z++) {
                RenderList.Add(new() {
                    pos = new Vector3(2f*x/testGridDensity + offsetX, 0f, -2f*z/testGridDensity + offsetZ),
                    mesh = _mesh_Sphere,
                    shader = _sh_Lit,
                    material = _m_Smooth,
                });

                Matrix4x4 mesh_m4x4 = Matrix4x4.CreateTranslation(
                    new Vector3(2f*x/testGridDensity + offsetX, 0f, -2f*z/testGridDensity + offsetZ));
                float[] mesh_uModel = Utils.MatrixToArray(mesh_m4x4);
                _sh_Lit.SetMatrix4("uModel", mesh_uModel);
                _sh_Lit.SetFloat("uRoughness", 1f - x/testGridDensity/testGridCount);
                _sh_Lit.SetFloat("uMetallic", z/testGridDensity/testGridCount);
                _mesh_Sphere.Draw();
            }
        }
    }


    private void OnRender (double deltaTime) {
        UpdateProjection();
        GL.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

        if (renderSkybox) _skybox?.Draw(View, Projection);

        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);

        /// Draw Scene
        Draw();

        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        /// Draw Gizmos
        if (Constants.drawGizmos) DrawGizmos();

        /// Draw UI
        DrawUI();

        DrawEnd();
    }
    private void UpdateProjection () {
        float aspect = Engine.Window.Size.X/(float)Engine.Window.Size.Y;
        Projection = Matrix4x4.CreatePerspectiveFieldOfView(Constants._cameraFOV, aspect, Constants._cameraPlaneClose, Constants._cameraPlaneFar);
        uView = Utils.MatrixToArray(View);
        uProjection = Utils.MatrixToArray(Projection);
    }

    private void DrawGizmos () {
        DrawGizmoGrid();
        DrawGizmoAxes();
        if (Constants.drawGizmosSun) DrawGizmoSun();

        DrawGizmoCameraOrbitCenter();
        DrawGizmoAxesWidget();
    }
    private void DrawGizmoGrid () {
        GL.DepthMask(false);

        GL.DepthRange(0.0001, 1.0);

        _sh_Grid.Use();
        SetSceneUniforms(_sh_Grid);
        _sh_Grid.SetMatrix4("uModel", _uModelIdentity);
        _sh_Grid.SetVector3("uCameraPos", Camera.Instance.cameraPos);
        _sh_Grid.SetVector3("uColor", Constants.lightGray);
        _sh_Grid.SetFloat("uAlpha", 0.5f);
        _sh_Grid.SetFloat("uRadius", 200f);
        _sh_Grid.SetFloat("uFade", 50f);
        _GizmoGrid.Draw();

        GL.DepthRange(0.0, 1.0);
        GL.DepthMask(true);
    }
    private void DrawGizmoAxes () {
        _sh_Axes.Use();
        SetSceneUniforms(_sh_Axes);
        _sh_Axes.SetMatrix4("uModel", _uModelIdentity);
        _sh_Axes.SetVector3("uCameraPos", Camera.Instance.cameraPos);
        _sh_Axes.SetFloat("uAlpha", 0.5f);
        _sh_Axes.SetFloat("uRadius", 200f);
        _sh_Axes.SetFloat("uFade", 50f);
        _GizmoAxes.Draw();
    }
    private void DrawGizmoSun () {
        GL.Disable(EnableCap.CullFace);

        _sh_Unlit.Use();
        SetSceneUniforms(_sh_Unlit);
        Matrix4x4 mesh_m4x4 = Utils.RotationFromDirection(Constants.sunLightDir)*Matrix4x4.Position(0f, 5f, 0f);
        float[] mesh_uModel = Utils.MatrixToArray(mesh_m4x4);
        _sh_Unlit.SetMatrix4("uModel", mesh_uModel);
        _sh_Unlit.SetColor("uColor", Constants.yellow);
        _sh_Unlit.SetFloat("uAlpha", 0.5f);
        _mesh_GizmoSun.Draw();

        GL.Enable(EnableCap.CullFace);
    }
    
    private void DrawGizmoCameraOrbitCenter () {
        if (CameraEditor.Instance is null) return;

        GL.DepthMask(false);

        _sh_Unlit.Use();
        SetSceneUniforms(_sh_Unlit);
        float dist = (CameraEditor.Instance.cameraOrbitCenterPos - Camera.Instance.cameraPos).Length();
        Matrix4x4 gizmoSphereModel = Matrix4x4.CreateScale(cameraOrbitCenterRadius*dist*0.01f)
            *Matrix4x4.CreateTranslation(CameraEditor.Instance.cameraOrbitCenterPos);
        _sh_Unlit.SetMatrix4("uModel", Utils.MatrixToArray(gizmoSphereModel));
        _sh_Unlit.SetVector3("uColor", 0.5f, 0.5f, 0.5f);
        _sh_Unlit.SetFloat("uAlpha", 0.2f);
        _mesh_Sphere.Draw();

        GL.DepthMask(true);
    }

    private void DrawGizmoAxesWidget () {
        const int gizmoSize = 90;
        const int gizmoMargin = 16;

        int windowWidth = Engine.Window.Size.X;
        int windowHeight = Engine.Window.Size.Y;

        int gizmoX = windowWidth - gizmoSize - gizmoMargin;
        int gizmoY = windowHeight - gizmoSize - gizmoMargin;

        GL.Viewport(gizmoX, gizmoY, (uint)gizmoSize, (uint)gizmoSize);
        GL.Clear(ClearBufferMask.DepthBufferBit);

        Matrix4x4 rotation = Camera.Instance.cameraRot;

        Vector3 forward = Vector3.Transform(Vector3.UnitZ, rotation);
        Vector3 up = Vector3.Transform(Vector3.UnitY, rotation);
        Vector3 gizmoCamPos = forward * 2.5f;
        Matrix4x4 gizmoView = Matrix4x4.CreateLookAt(
            gizmoCamPos,
            Vector3.Zero,
            up
        );

        Matrix4x4 gizmoProjection = Matrix4x4.CreateOrthographic(2.2f, 2.2f, 0.1f, 10f);

        GL.Disable(EnableCap.DepthTest);

        _sh_Axes.Use();
        _sh_Axes.SetMatrix4("uModel", Utils.MatrixToArray(Matrix4x4.Identity));
        _sh_Axes.SetMatrix4("uView", Utils.MatrixToArray(gizmoView));
        _sh_Axes.SetMatrix4("uProjection", Utils.MatrixToArray(gizmoProjection));

        _GizmoAxesWidget.Draw();

        GL.Enable(EnableCap.DepthTest);

        GL.Viewport(Engine.Window.Size);
    }

    private void DrawUI () {
        GL.Disable(EnableCap.CullFace);

        if (Inputs.Actions[Inputs.F3].pressedDown) _renderFPS = !_renderFPS;
        if (_renderFPS) {
            int left = 10;
            _textRenderer.DrawText($"FPS: {(int)(1/Engine.deltaTime)}", left, 20, Engine.Window.Size.X, Engine.Window.Size.Y);
            _textRenderer.DrawText($"ms: {Engine.deltaTime*1000:F1}", left, 40, Engine.Window.Size.X, Engine.Window.Size.Y);
            _textRenderer.DrawText($"Pos: {Camera.Instance.cameraPos:F2}", left, 60, Engine.Window.Size.X, Engine.Window.Size.Y);
            _textRenderer.DrawText($"MousePos: {Camera.Instance.mousePos:F2}", left, 80, Engine.Window.Size.X, Engine.Window.Size.Y);
            _textRenderer.DrawText($"Components: {ComponentManager.componentsCount}", left, 100, Engine.Window.Size.X, Engine.Window.Size.Y);
            //_textRenderer.DrawText($"Wheel: {Inputs.Wheel:F2}", left, 100, Engine.Window.Size.X, Engine.Window.Size.Y);
            
            //Log.log(ComponentManager.NameAll);
        }

        GL.Enable(EnableCap.CullFace);
    }

    private void DrawEnd () {
        RenderList.Clear();
    }

    internal void OnFrameBufferResize (Silk.NET.Maths.Vector2D<int> newSize) {
        GL.Viewport(newSize);
        if (0 < newSize.X && 0 < newSize.Y) 
            UpdateProjection();
    }

    internal void OnClosing () {
        _mesh_Cube.Dispose();
        _mesh_Sphere.Dispose();
        _GizmoGrid.Dispose();
        _GizmoAxes.Dispose();
        _GizmoAxesWidget.Dispose();
        _mesh_GizmoSun.Dispose();
        _sh_Lit.Dispose();
        _sh_Unlit.Dispose();
        _sh_Grid.Dispose();
        _sh_Axes.Dispose();
        _skybox.Dispose();
        _hdrTexture_Skybox?.Dispose();
        _sh_Skybox.Dispose();
        _textRenderer.Dispose();
    }

}
