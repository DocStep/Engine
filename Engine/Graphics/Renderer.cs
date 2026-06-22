using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Silk.NET.Maths;
using Engine.Graphics.UI;

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
        _gizmoSphere = new Mesh(Sphere.Generate());
        _grid = new WorldGrid((int)_cameraPlaneFar, 1f);
        _axes = new WorldAxes(10f*_cameraPlaneFar);
        _gizmoAxes = new WorldAxes(1f);

        _shader = new Shader(Utils.LoadSrc("src/Shaders/Vertex.shader"), Utils.LoadSrc("src/Shaders/Fragment.shader"), "Lit");
        _shaderUnlit = new Shader(Utils.LoadSrc("src/Shaders/UnlitVertex.shader"), Utils.LoadSrc("src/Shaders/UnlitFragment.shader"), "Unlit");
        _shaderGrid = new Shader(Utils.LoadSrc("src/Shaders/GridVertex.shader"), Utils.LoadSrc("src/Shaders/GridFragment.shader"), "Grid");
        _shaderAxes = new Shader(Utils.LoadSrc("src/Shaders/AxesVertex.shader"), Utils.LoadSrc("src/Shaders/AxesFragment.shader"), "Axes");

        _shaderSkybox = new Shader(Utils.LoadSrc("src/Shaders/SkyboxVertex.shader"), Utils.LoadSrc("src/Shaders/SkyboxFragment.shader"), "Skybox");
        //_hdrTexture = new HdrTexture("src/hdr/autumn_field_puresky_4k.hdr");
        _hdrTexture = new HdrTexture("src/hdr/rogland_clear_night_4k.hdr");
        ///_hdrTexture = new HdrTexture("src/hdr/grasslands_sunset_4k.hdr");
        ///_hdrTexture = new HdrTexture("src/hdr/overcast_soil_puresky_4k.hdr");
        //_hdrTexture = new HdrTexture("src/hdr/qwantani_dusk_2_puresky_4k.hdr");
        _skybox = new Skybox(_shaderSkybox, _hdrTexture);
        _skybox.BlurScale = 2f;

        _mat_Default = new Material { Color = Constants.lightGray, };
        _mat_Smooth = new Material { Color = Constants.lightGray, Roughness = 0f, };
        _mat_Matt = new Material { Color = Constants.lightGray, Roughness = 1f, };
        _mat_Metallic = new Material { Color = Constants.gray, Roughness = 0.05f, Metallic = 1, };

        _mat_DefaultUnlit = new Material { Color = Constants.gray, Roughness = 1f, };

        _mat_DefaultAxes = new Material { Color = Constants.gray, };

        _mat_Red = new Material { Color = new(1, 0, 0), };
        _mat_Green = new Material { Color = new(0, 1, 0), };
        _mat_Blue = new Material { Color = new(0, 0, 1), };

        _textRenderer = new TextRenderer();
    }

    public static Renderer Instance = null!;

    public readonly TextRenderer _textRenderer = null!;

    public Action? DrawGizmos = null;


    internal readonly GL GL = null!;

    internal Shader _shader = null!;
    internal Shader _shaderUnlit = null!;
    internal Shader _shaderGrid = null!;
    internal Shader _shaderAxes = null!;

    internal Shader _shaderSkybox = null!;
    private Skybox _skybox = null!;
    private HdrTexture? _hdrTexture = null;

    internal Material _mat_Default = null!;
    internal Material _mat_Smooth = null!;
    internal Material _mat_Matt = null!;
    internal Material _mat_Metallic = null!;

    internal Material _mat_DefaultUnlit = null!;

    internal Material _mat_DefaultAxes = null!;

    internal Material _mat_Red = null!;
    internal Material _mat_Green = null!;
    internal Material _mat_Blue = null!;

    private Mesh _cube = null!;
    private Mesh _sphere = null!;
    private Mesh _gizmoSphere = null!;
    private WorldGrid _grid = null!;
    private WorldAxes _axes = null!;
    private WorldAxes _gizmoAxes = null!;

    internal Vector3D<float> sunLightDir = Vector3D.Normalize(new Vector3D<float>(0.4f, -1f, -0.3f));
    internal Vector3D<float> sunLightColor = new Vector3D<float>(1f, 1f, 1f);
    internal float sunLightIntensity = 1f;

    internal const float _cameraFOV = 0.25f*MathF.PI;
    internal const float _cameraPlaneClose = 0.1f;
    internal const float _cameraPlaneFar = 1000f;

    private readonly static Matrix4X4<float> _modelIdentity = Matrix4X4<float>.Identity;
    private readonly static float[] _uModelIdentity = Utils.MatrixToArray(_modelIdentity);
    
    /// Gizmos
    private float cameraOrbitCenterRadius = 0.5f;

    /// Debug
    internal Matrix4X4<float> View = Matrix4X4<float>.Identity;
    internal Matrix4X4<float> Projection = Matrix4X4<float>.Identity;
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


    private void Draw () {
        sunLightIntensity = 10f;
        Matrix4X4<float> mesh_m4x4;
        float[] mesh_uModel;

        /// Cube
        SetSceneUniforms(_shader);
        mesh_m4x4 = Matrix4X4.CreateTranslation(new Vector3D<float>(4f, 0f, 0f));
        mesh_uModel = Utils.MatrixToArray(mesh_m4x4);
        _shader.SetMatrix4("uModel", _uModelIdentity);
        _mat_Default.Apply(_shader);
        _cube.Draw();

        /// Sphere R
        SetSceneUniforms(_shader);
        mesh_m4x4 = Matrix4X4.CreateTranslation(new Vector3D<float>(2f, 0f, 0f));
        mesh_uModel = Utils.MatrixToArray(mesh_m4x4);
        _shader.SetMatrix4("uModel", mesh_uModel);
        _mat_Red.Apply(_shader);
        _sphere.Draw();

        /// Sphere R
        SetSceneUniforms(_shader);
        mesh_m4x4 = Matrix4X4.CreateTranslation(new Vector3D<float>(0f, 0f, 2f));
        mesh_uModel = Utils.MatrixToArray(mesh_m4x4);
        _shader.SetMatrix4("uModel", mesh_uModel);
        _mat_Red.Apply(_shader);
        _sphere.Draw();

        /// Sphere G
        SetSceneUniforms(_shader);
        mesh_m4x4 = Matrix4X4.CreateTranslation(new Vector3D<float>(2f, 0f, 2f));
        mesh_uModel = Utils.MatrixToArray(mesh_m4x4);
        _shader.SetMatrix4("uModel", mesh_uModel);
        _mat_Green.Apply(_shader);
        _sphere.Draw();

        /// Sphere B
        SetSceneUniforms(_shader);
        mesh_m4x4 = Matrix4X4.CreateTranslation(new Vector3D<float>(4f, 0f, 2f));
        mesh_uModel = Utils.MatrixToArray(mesh_m4x4);
        _shader.SetMatrix4("uModel", mesh_uModel);
        _mat_Blue.Apply(_shader);
        _sphere.Draw();

        /// Sphere Smooth
        SetSceneUniforms(_shader);
        mesh_m4x4 = Matrix4X4.CreateTranslation(new Vector3D<float>(2f, 0f, -2f));
        mesh_uModel = Utils.MatrixToArray(mesh_m4x4);
        _shader.SetMatrix4("uModel", mesh_uModel);
        _mat_Smooth.Apply(_shader);
        _sphere.Draw();

        /// Sphere Matt
        SetSceneUniforms(_shader);
        mesh_m4x4 = Matrix4X4.CreateTranslation(new Vector3D<float>(0f, 0f, -2f));
        mesh_uModel = Utils.MatrixToArray(mesh_m4x4);
        _shader.SetMatrix4("uModel", mesh_uModel);
        _mat_Matt.Apply(_shader);
        _sphere.Draw();


        /// Spheres Grid
        float offsetX = 0f;
        float offsetZ = -4f;
        float gridCount = 5f;
        float gridScale = 1f;
        SetSceneUniforms(_shader);
        _shader.SetColor("uColor", Constants.gray);
        _shader.SetFloat("uExposure", 1f);
        for (int x = 0; x < gridCount*gridScale; x++) {
            for (int z = 0; z < gridCount*gridScale; z++) {
                mesh_m4x4 = Matrix4X4.CreateTranslation(
                    new Vector3D<float>(2f*x/gridScale + offsetX, 0f, -2f*z/gridScale + offsetZ));
                mesh_uModel = Utils.MatrixToArray(mesh_m4x4);
                _shader.SetMatrix4("uModel", mesh_uModel);
                _shader.SetFloat("uRoughness", 1f - x/gridCount/gridScale);
                _shader.SetFloat("uMetallic", z/gridCount/gridScale);
                _sphere.Draw();
            }
        }

        SetSceneUniforms(_shader);
        mesh_m4x4 = Matrix4X4.CreateTranslation(new Vector3D<float>(-4f, 0f, 0f))
            *Matrix4X4.CreateScale<float>(2f, 2f, 2f);
        mesh_uModel = Utils.MatrixToArray(mesh_m4x4);
        _shader.SetMatrix4("uModel", mesh_uModel);
        _mat_Matt.Apply(_shader);
        _shader.SetFloat("uRoughness", 0);
        _shader.SetFloat("uMetallic", 1);
        _sphere.Draw();
    }


    private void OnRender (double deltaTime) {
        UpdateProjection();

        GL.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

        if (renderSkybox) _skybox?.Draw(View, Projection);

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
        Projection = Matrix4X4.CreatePerspectiveFieldOfView(_cameraFOV, aspect, _cameraPlaneClose, _cameraPlaneFar);
        uView = Utils.MatrixToArray(View);
        uProjection = Utils.MatrixToArray(Projection);
    }
    private void DrawUI () {
        GL.Disable(EnableCap.CullFace);
        
        if (_renderFPS) {
            _textRenderer.DrawText($"FPS: {(int)(1/Engine.deltaTime)}", 20, 30, Engine.Window.Size.X, Engine.Window.Size.Y);
            _textRenderer.DrawText($"ms: {Engine.deltaTime*1000:F1}", 20, 50, Engine.Window.Size.X, Engine.Window.Size.Y);
        }
            

        GL.Enable(EnableCap.CullFace);
    }

    private void DrawGizmosBasic () {
        DrawGizmoGrid();
        DrawGizmoAxes();
    }
    private void DrawGizmoGrid () {
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
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
        _grid.Draw();

        GL.DepthRange(0.0, 1.0);
        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
    }
    private void DrawGizmoAxes () {
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _shaderAxes.Use();
        SetSceneUniforms(_shaderAxes);
        _shaderAxes.SetMatrix4("uModel", _uModelIdentity);
        _shaderAxes.SetVector3("uCameraPos", Camera.Instance.cameraPos);
        _shaderAxes.SetFloat("uAlpha", 0.5f);
        _shaderAxes.SetFloat("uRadius", 200f);
        _shaderAxes.SetFloat("uFade", 50f);
        _axes.Draw();

        GL.Disable(EnableCap.Blend);
    }
    private void DrawGizmoCameraOrbitCenter () {
        if (CameraEditor.Instance is null) return;

        _shaderUnlit.Use();
        SetSceneUniforms(_shaderUnlit);
        float dist = (CameraEditor.Instance.cameraOrbitCenterPos - Camera.Instance.cameraPos).Length;
        Matrix4X4<float> gizmoSphereModel = Matrix4X4.CreateScale(cameraOrbitCenterRadius*dist*0.01f)
            *Matrix4X4.CreateTranslation(CameraEditor.Instance.cameraOrbitCenterPos);
        _shaderUnlit.SetMatrix4("uModel", Utils.MatrixToArray(gizmoSphereModel));
        _shaderUnlit.SetVector3("uColor", 0.5f, 0.5f, 0.5f);
        _shaderUnlit.SetFloat("uAlpha", 0.2f);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.DepthMask(false);
        _gizmoSphere.Draw();
        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
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

        Matrix4X4<float> rotation = Camera.Instance.cameraRot;

        Vector3D<float> forward = Vector3D.Transform(Vector3D<float>.UnitZ, rotation);
        Vector3D<float> up = Vector3D.Transform(Vector3D<float>.UnitY, rotation);
        Vector3D<float> gizmoCamPos = forward * 2.5f;
        Matrix4X4<float> gizmoView = Matrix4X4.CreateLookAt(
            gizmoCamPos,
            Vector3D<float>.Zero,
            up
        );

        Matrix4X4<float> gizmoProjection = Matrix4X4.CreateOrthographic(2.2f, 2.2f, 0.1f, 10f);

        GL.Disable(EnableCap.DepthTest);

        _shaderAxes.Use();
        _shaderAxes.SetMatrix4("uModel", Utils.MatrixToArray(Matrix4X4<float>.Identity));
        _shaderAxes.SetMatrix4("uView", Utils.MatrixToArray(gizmoView));
        _shaderAxes.SetMatrix4("uProjection", Utils.MatrixToArray(gizmoProjection));

        _gizmoAxes.Draw();

        GL.Enable(EnableCap.DepthTest);

        GL.Viewport(Engine.Window.Size);
    }



    internal void OnFramebufferResize (Vector2D<int> newSize) {
        GL.Viewport(newSize);
        if (0 < newSize.X && 0 < newSize.Y) 
            UpdateProjection();
    }

    internal void OnClosing () {
        _cube.Dispose();
        _sphere.Dispose();
        _gizmoSphere.Dispose();
        _grid.Dispose();
        _axes.Dispose();
        _gizmoAxes.Dispose();
        _shader.Dispose();
        _shaderUnlit.Dispose();
        _shaderAxes.Dispose();
        _skybox.Dispose();
        _hdrTexture?.Dispose();
        _shaderSkybox.Dispose();
    }

}
