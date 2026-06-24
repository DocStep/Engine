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
        Engine.Window.FramebufferResize += OnFramebufferResize;

        GL = Engine.Window.CreateOpenGL();
        GL.ClearColor(0.1f, 0.1f, 0.15f, 1f);
        GL.Enable(EnableCap.DepthTest);

        _cube = new Mesh(Cube.Generate());
        _sphere = new Mesh(Sphere.Generate());
        _gizmoGrid = new WorldGrid((int)_cameraPlaneFar, 1f);
        _gizmoAxes = new WorldAxes(10f*_cameraPlaneFar);
        _gizmoAxesWidget = new WorldAxes(1f);
        _gizmoSun = new Mesh(Arrow.Generate(shaftLength: 1f, shaftRadius: 0.01f, headLength: 0.2f, headRadius: 0.1f));
        _mesh_gizmoCube = new Mesh(WireGizmos.Cube(Vector3.Zero, Vector3.One));
        _mesh_gizmoSphere = new Mesh(WireGizmos.Sphere(Vector3.Zero, 0.5f));
        _mesh_gizmoCapsule = new Mesh(WireGizmos.Capsule(-0.5f*Vector3.UnitY, 0.5f*Vector3.UnitY, 0.5f));

        _gizmoSphereOrbit = new Mesh(Sphere.Generate());

        _shaderLit = new Shader(Utils.LoadSrc("src/Shaders/Lit_Vertex.shader"), Utils.LoadSrc("src/Shaders/Lit_Fragment.shader"), "Lit");
        _shaderUnlit = new Shader(Utils.LoadSrc("src/Shaders/Unlit_Vertex.shader"), Utils.LoadSrc("src/Shaders/Unlit_Fragment.shader"), "Unlit");
        _shaderGrid = new Shader(Utils.LoadSrc("src/Shaders/Grid_Vertex.shader"), Utils.LoadSrc("src/Shaders/Grid_Fragment.shader"), "Grid");
        _shaderAxes = new Shader(Utils.LoadSrc("src/Shaders/Axes_Vertex.shader"), Utils.LoadSrc("src/Shaders/Axes_Fragment.shader"), "Axes");

        _shaderSkybox = new Shader(Utils.LoadSrc("src/Shaders/Skybox_Vertex.shader"), Utils.LoadSrc("src/Shaders/Skybox_Fragment.shader"), "Skybox");
        //_hdrTexture = new HdrTexture("src/hdr/autumn_field_puresky_4k.hdr");
        _hdrTexture = new HdrTexture("src/hdr/rogland_clear_night_4k.hdr");
        ///_hdrTexture = new HdrTexture("src/hdr/grasslands_sunset_4k.hdr");
        ///_hdrTexture = new HdrTexture("src/hdr/overcast_soil_puresky_4k.hdr");
        //_hdrTexture = new HdrTexture("src/hdr/qwantani_dusk_2_puresky_4k.hdr");
        _skybox = new Skybox(_shaderSkybox, _hdrTexture);
        _skybox.BlurScale = 3f;

        _mat_Lit = new Material { Color = Constants.lightGray, };
        _mat_Smooth = new Material { Color = Constants.lightGray, Roughness = 0f, };
        _mat_Matt = new Material { Color = Constants.lightGray, Roughness = 1f, };
        _mat_Metallic = new Material { Color = Constants.gray, Roughness = 0.05f, Metallic = 1, };
        _mat_MaterialPreview = new Material { Color = Constants.white, Roughness = 0, Metallic = 1, Ambient = 0 };

        _mat_Unlit = new Material { Color = Constants.gray, Roughness = 1f, };

        _mat_Axes = new Material { Color = Constants.gray, };
        _mat_GizmosG = new Material { Color = Constants.green, Alpha = 0.5f, };

        _mat_LitRed = new Material { Color = new(1, 0, 0), };
        _mat_LitGreen = new Material { Color = new(0, 1, 0), };
        _mat_LitBlue = new Material { Color = new(0, 0, 1), };

        _mesh_Torus = new Mesh(ObjLoader.Load("src/Models/Torus.obj"));
        _mesh_Suzanne = new Mesh(ObjLoader.Load("src/Models/Suzanne.obj"));
        _mesh_SuzanneHighRes = new Mesh(ObjLoader.Load("src/Models/SuzanneHighRes.obj"));

        _textRenderer = new TextRenderer();
    }

    public static Renderer Instance = null!;

    public readonly TextRenderer _textRenderer = null!;

    public Action? DrawGizmos = null;


    internal readonly GL GL = null!;

    internal readonly Shader _shaderLit = null!;
    internal readonly Shader _shaderUnlit = null!;
    internal readonly Shader _shaderGrid = null!;
    internal readonly Shader _shaderAxes = null!;

    internal Shader _shaderSkybox = null!;
    private Skybox _skybox = null!;
    private HdrTexture? _hdrTexture = null;

    internal Material _mat_Lit = null!;
    internal Material _mat_Unlit = null!;
    internal Material _mat_Smooth = null!;
    internal Material _mat_Matt = null!;
    internal Material _mat_Metallic = null!;
    internal Material _mat_MaterialPreview = null!;

    internal Material _mat_Axes = null!;
    internal Material _mat_GizmosG = null!;

    internal Material _mat_LitRed = null!;
    internal Material _mat_LitGreen = null!;
    internal Material _mat_LitBlue = null!;

    private Mesh _cube = null!;
    private Mesh _sphere = null!;
    private Mesh _gizmoSphereOrbit = null!;
    private WorldGrid _gizmoGrid = null!;
    private WorldAxes _gizmoAxes = null!;
    private WorldAxes _gizmoAxesWidget = null!;
    private Mesh _gizmoSun = null!;

    private Mesh _mesh_Torus = null!;
    private Mesh _mesh_Suzanne = null!;
    private Mesh _mesh_SuzanneHighRes = null!;

    private Mesh _mesh_gizmoCube = null!; 
    private Mesh _mesh_gizmoSphere = null!; 
    private Mesh _mesh_gizmoCapsule = null!; 

    internal Vector3 sunLightDir = Vector3.Normalize(new Vector3(0.4f, -1f, -0.3f));
    internal Vector3 sunLightColor = new Vector3(1f, 1f, 1f);
    internal float sunLightIntensity = 1f;

    internal const float _cameraFOV = 0.25f*MathF.PI;
    internal const float _cameraPlaneClose = 0.1f;
    internal const float _cameraPlaneFar = 1000f;

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



    private void SetSceneUniforms (Shader shader) {
        shader.Use();
        shader.SetMatrix4("uView", uView);
        shader.SetMatrix4("uProjection", uProjection);
        shader.SetVector3("uSunLightColor", sunLightColor.X, sunLightColor.Y, sunLightColor.Z);
        shader.SetFloat("uSunLightIntensity", sunLightIntensity);
        shader.SetVector3("uSunLightDir", sunLightDir.X, sunLightDir.Y, sunLightDir.Z);
        shader.SetVector3("uViewPos", Camera.Instance.cameraPos.X, Camera.Instance.cameraPos.Y, Camera.Instance.cameraPos.Z);
        shader.SetVector3("uAmbientColor", 0.05f, 0.05f, 0.06f);

        if (_hdrTexture is not null) {
            _hdrTexture.Bind(TextureUnit.Texture0);
            shader.SetInt("uSkybox", 0);

            float maxLod = MathF.Log2(MathF.Max(_hdrTexture.Width, _hdrTexture.Height));
            shader.SetFloat("uMaxReflectionLod", maxLod);
        }
    }

    const string GameObjectName = "GameObject";
    public struct RenderInfo () {
        public string name = GameObjectName;
        public Vector3 pos = default;
        public Vector3 rot = default;
        public Vector3 scale = default;
        public Mesh mesh = default!;
        public Shader shader = default!;
        public Material? material = default;
        public PrimitiveType primitiveType = PrimitiveType.Triangles;
    }
    private void Draw () {
        sunLightIntensity = 10f;
        List<RenderInfo> List = new List<RenderInfo>() {
            new() { name = "Cube", pos = new Vector3(0, 0, -4), mesh = _cube, shader = _shaderLit, material = _mat_Lit, },
            new() { name = "Sphere", pos = new Vector3(2, 0, -4), mesh = _sphere, shader = _shaderLit, material = _mat_Lit, },

            new() { name = "Sphere R", pos = new Vector3(0, 0, -6), mesh = _sphere, shader = _shaderLit, material = _mat_LitRed, },
            new() { name = "Sphere G", pos = new Vector3(2, 0, -6), mesh = _sphere, shader = _shaderLit, material = _mat_LitGreen, },
            new() { name = "Sphere B", pos = new Vector3(4, 0, -6), mesh = _sphere, shader = _shaderLit, material = _mat_LitBlue, },

            new() { name = "Sphere Matt", pos = new Vector3(0, 0, -8), mesh = _sphere, shader = _shaderLit, material = _mat_Smooth, },
            new() { name = "Sphere Smooth", pos = new Vector3(2, 0, -8), mesh = _sphere, shader = _shaderLit, material = _mat_Smooth, },

            new() { name = "Reflection Sphere", pos = new Vector3(-4, 0, 0), scale = 2*Vector3.One, 
                mesh = _sphere, shader = _shaderLit, material = _mat_MaterialPreview, },
            new() { name = "Reflection SuzanneHightRes", pos = new Vector3(0, 0, 0), rot = new(0, 180, 0),
                mesh = _mesh_SuzanneHighRes, shader = _shaderLit, material = _mat_MaterialPreview, },
            new() { name = "Reflection Suzanne", pos = new Vector3(4, 0, 0), rot = new(0, 180, 0),
                mesh = _mesh_Suzanne, shader = _shaderLit, material = _mat_MaterialPreview, },
            new() { name = "Reflection Torus", pos = new Vector3(8, 0, 0),
                mesh = _mesh_Torus, shader = _shaderLit, material = _mat_MaterialPreview, },

            new() { name = "Gizmos Cube", pos = new Vector3(0, 0, 4),
                mesh = _mesh_gizmoCube, shader = _shaderUnlit, material = _mat_GizmosG, primitiveType = PrimitiveType.Lines, },
            new() { name = "Gizmos Sphere", pos = new Vector3(2, 0, 4),
                mesh = _mesh_gizmoSphere, shader = _shaderUnlit, material = _mat_GizmosG, primitiveType = PrimitiveType.Lines, },
            new() { name = "Gizmos Capsule", pos = new Vector3(4, 0, 4),
                mesh = _mesh_gizmoCapsule, shader = _shaderUnlit, material = _mat_GizmosG, primitiveType = PrimitiveType.Lines, },
        };

        /// Spheres Grid
        float offsetX = 0f;
        float offsetZ = -10f;
        float gridCount = 5f;
        float gridScale = 1f;
        SetSceneUniforms(_shaderLit);
        _shaderLit.SetColor("uColor", Constants.black);
        _shaderLit.SetFloat("uExposure", 1f);
        for (int x = 0; x < gridCount*gridScale; x++) {
            for (int z = 0; z < gridCount*gridScale; z++) {
                List.Add(new() {
                    name = "SphereSmooth",
                    pos = new Vector3(2f*x/gridScale + offsetX, 0f, -2f*z/gridScale + offsetZ),
                    mesh = _sphere,
                    shader = _shaderLit,
                    material = _mat_Smooth,
                });

                Matrix4x4 mesh_m4x4 = Matrix4x4.CreateTranslation(
                    new Vector3(2f*x/gridScale + offsetX, 0f, -2f*z/gridScale + offsetZ));
                float[]  mesh_uModel = Utils.MatrixToArray(mesh_m4x4);
                _shaderLit.SetMatrix4("uModel", mesh_uModel);
                _shaderLit.SetFloat("uRoughness", 1f - x/gridCount/gridScale);
                _shaderLit.SetFloat("uMetallic", z/gridCount/gridScale);
                _sphere.Draw();
            }
        }

        int count = List.Count;
        for (int i = 0; i < List.Count; i++) {
            Matrix4x4 mesh_m4x4 = Matrix4x4.Rotation(List[i].rot)*Matrix4x4.Position(List[i].pos);
            float[] mesh_uModel = Utils.MatrixToArray(mesh_m4x4);
            SetSceneUniforms(List[i].shader);
            List[i].shader.SetMatrix4("uModel", mesh_uModel);
            List[i].material?.Apply(List[i].shader);
            List[i].mesh.Draw(List[i].primitiveType);
        }
    }


    private void OnRender (double deltaTime) {
        UpdateProjection();

        GL.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

        if (renderSkybox) {
            _skybox?.Draw(View, Projection);
        }

        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);

        Draw();

        DrawGizmosBasic();
        ///DrawGizmos?.Invoke();

        DrawGizmoCameraOrbitCenter();
        DrawGizmoAxesWidget();

        DrawUI();
    }
    private void UpdateProjection () {
        float aspect = Engine.Window.Size.X/(float)Engine.Window.Size.Y;
        Projection = Matrix4x4.CreatePerspectiveFieldOfView(_cameraFOV, aspect, _cameraPlaneClose, _cameraPlaneFar);
        uView = Utils.MatrixToArray(View);
        uProjection = Utils.MatrixToArray(Projection);
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
            //_textRenderer.DrawText($"Wheel: {Inputs.Wheel:F2}", left, 100, Engine.Window.Size.X, Engine.Window.Size.Y);
        }

        GL.Enable(EnableCap.CullFace);
    }

    private void DrawGizmosBasic () {
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        DrawGizmoGrid();
        DrawGizmoAxes();
        DrawGizmoSun();

        GL.Disable(EnableCap.Blend);
    }
    private void DrawGizmoGrid () {
        GL.DepthMask(false);

        GL.DepthRange(0.0001, 1.0);

        _shaderGrid.Use();
        SetSceneUniforms(_shaderGrid);
        _shaderGrid.SetMatrix4("uModel", _uModelIdentity);
        _shaderGrid.SetVector3("uCameraPos", Camera.Instance.cameraPos);
        _shaderGrid.SetVector3("uColor", Constants.lightGray);
        _shaderGrid.SetFloat("uAlpha", 0.5f);
        _shaderGrid.SetFloat("uRadius", 200f);
        _shaderGrid.SetFloat("uFade", 50f);
        _gizmoGrid.Draw();

        GL.DepthRange(0.0, 1.0);
        GL.DepthMask(true);
    }
    private void DrawGizmoAxes () {
        _shaderAxes.Use();
        SetSceneUniforms(_shaderAxes);
        _shaderAxes.SetMatrix4("uModel", _uModelIdentity);
        _shaderAxes.SetVector3("uCameraPos", Camera.Instance.cameraPos);
        _shaderAxes.SetFloat("uAlpha", 0.5f);
        _shaderAxes.SetFloat("uRadius", 200f);
        _shaderAxes.SetFloat("uFade", 50f);
        _gizmoAxes.Draw();
    }
    private void DrawGizmoSun () {
        GL.Disable(EnableCap.CullFace);

        _shaderUnlit.Use();
        SetSceneUniforms(_shaderUnlit);
        //Matrix4X4<float> mesh_m4x4 = Matrix4X4.CreateTranslation(new Vector3D<float>(0f, 5f, 0f))
        //    *Matrix4X4.CreateRotationX(sunLightDir.X)*Matrix4X4.CreateRotationY(sunLightDir.Y)*Matrix4X4.CreateRotationZ(sunLightDir.Z);
        Matrix4x4 mesh_m4x4 = Matrix4x4.Rotation(sunLightDir)*Matrix4x4.CreateTranslation(new Vector3(-8f, 5f, 0f));

        float[] mesh_uModel = Utils.MatrixToArray(mesh_m4x4);
        _shaderUnlit.SetMatrix4("uModel", mesh_uModel);
        _shaderUnlit.SetColor("uColor", Constants.yellow);
        _shaderUnlit.SetFloat("uAlpha", 0.5f);
        _gizmoSun.Draw();

        GL.Enable(EnableCap.CullFace);
    }
    
    private void DrawGizmoCameraOrbitCenter () {
        if (CameraEditor.Instance is null) return;

        GL.DepthMask(false);

        _shaderUnlit.Use();
        SetSceneUniforms(_shaderUnlit);
        float dist = (CameraEditor.Instance.cameraOrbitCenterPos - Camera.Instance.cameraPos).Length();
        Matrix4x4 gizmoSphereModel = Matrix4x4.CreateScale(cameraOrbitCenterRadius*dist*0.01f)
            *Matrix4x4.CreateTranslation(CameraEditor.Instance.cameraOrbitCenterPos);
        _shaderUnlit.SetMatrix4("uModel", Utils.MatrixToArray(gizmoSphereModel));
        _shaderUnlit.SetVector3("uColor", 0.5f, 0.5f, 0.5f);
        _shaderUnlit.SetFloat("uAlpha", 0.2f);
        _gizmoSphereOrbit.Draw();

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

        _shaderAxes.Use();
        _shaderAxes.SetMatrix4("uModel", Utils.MatrixToArray(Matrix4x4.Identity));
        _shaderAxes.SetMatrix4("uView", Utils.MatrixToArray(gizmoView));
        _shaderAxes.SetMatrix4("uProjection", Utils.MatrixToArray(gizmoProjection));

        _gizmoAxesWidget.Draw();

        GL.Enable(EnableCap.DepthTest);

        GL.Viewport(Engine.Window.Size);
    }



    internal void OnFramebufferResize (Silk.NET.Maths.Vector2D<int> newSize) {
        GL.Viewport(newSize);
        if (0 < newSize.X && 0 < newSize.Y) 
            UpdateProjection();
    }

    internal void OnClosing () {
        _cube.Dispose();
        _sphere.Dispose();
        _gizmoSphereOrbit.Dispose();
        _gizmoGrid.Dispose();
        _gizmoAxes.Dispose();
        _gizmoAxesWidget.Dispose();
        _gizmoSun.Dispose();
        _shaderLit.Dispose();
        _shaderUnlit.Dispose();
        _shaderGrid.Dispose();
        _shaderAxes.Dispose();
        _skybox.Dispose();
        _hdrTexture?.Dispose();
        _shaderSkybox.Dispose();
        _textRenderer.Dispose();
    }

}
