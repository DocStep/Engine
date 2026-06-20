using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace Engine.Graphics;


internal class Renderer {
    public Renderer (IWindow Window) {
        Instance = this;
        this.Window = Window;

        Window.Render += OnRender;
        Window.Closing += OnClosing;
        Window.FramebufferResize += OnFramebufferResize;

        GL = Window.CreateOpenGL();
        GL.ClearColor(0.1f, 0.1f, 0.15f, 1f);
        GL.Enable(EnableCap.DepthTest);

        _mat_Default = new Material { Color = Constants.lightGray, Roughness = 0.5f };
        _mat_Default1 = new Material { Color = Constants.gray, Roughness = 1f };
        _mat_Default2 = new Material { Color = Constants.darkGray, Roughness = 0f };
        _mat_DefaultUnlit = new Material { Color = new(0.6f, 0.55f, 0.5f), Roughness = 1f };
        _mat_DefaultAxes = new Material { Color = new(0.6f, 0.55f, 0.5f), Roughness = 0.5f };

        _cube = new Cube(GL);
        _sphere = new Sphere(GL);
        _gizmoSphere = new Sphere(GL);
        _grid = new WorldGrid(GL, 10, 1f);
        _axes = new WorldAxes(GL, 1000f);
        _gizmoAxes = new WorldAxes(GL, 1f);

        _shader = new Shader(GL, Utils.LoadSrc("src/Shaders/Vertex.shader"), Utils.LoadSrc("src/Shaders/Fragment.shader"));
        _shaderUnlit = new Shader(GL, Utils.LoadSrc("src/Shaders/UnlitVertex.shader"), Utils.LoadSrc("src/Shaders/UnlitFragment.shader"));
        _shaderAxes = new Shader(GL, Utils.LoadSrc("src/Shaders/AxesVertex.shader"), Utils.LoadSrc("src/Shaders/AxesFragment.shader"));
    }

    public static Renderer Instance = null!;

    public Action? DrawGizmos = null;


    private IWindow Window = null!;
    internal GL GL = null!;

    internal Shader _shader = null!;
    internal Shader _shaderUnlit = null!;
    internal Shader _shaderAxes = null!;

    internal Material _mat_Default = null!;
    internal Material _mat_Default1 = null!;
    internal Material _mat_Default2 = null!;
    internal Material _mat_DefaultUnlit = null!;
    internal Material _mat_DefaultAxes = null!;

    private Cube _cube = null!;
    private Sphere _sphere = null!;
    private Sphere _gizmoSphere = null!;
    private WorldGrid _grid = null!;
    private WorldAxes _axes = null!;
    private WorldAxes _gizmoAxes = null!;

    internal Vector3D<float> sunLightDir = Vector3D.Normalize(new Vector3D<float>(-0.4f, -1f, -0.3f));
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
    internal Matrix4X4<float> View;
    internal Matrix4X4<float> Projection;
    private float[] uView;
    private float[] uProjection;


    void Draw () {
        sunLightIntensity = 10f;

        /// Cube
        _shader.Use();
        SetSceneUniforms(_shader);
        //Matrix4X4<float> cubeModel = Matrix4X4.CreateRotationX(0.25f*MathF.PI)*Matrix4X4.CreateRotationY(0.25f*MathF.PI);
        _shader.SetMatrix4("uModel", _uModelIdentity);
        _mat_Default.Apply(_shader);
        //_shader.SetFloat("uAmbient", 1f);
        _cube.Draw();

        /// Sphere
        _shader.Use();
        Matrix4X4<float> sphere_Model = Matrix4X4.CreateScale(0.5f)*Matrix4X4.CreateTranslation(new Vector3D<float>(1.5f, 0f, 0f));
        _shader.SetMatrix4("uModel", Utils.MatrixToArray(sphere_Model));
        _mat_Default2.Apply(_shader);
        _shader.SetColor("uColor", Constants.cyan);
        _sphere.Draw();
    }


    private void OnRender (double deltaTime) {
        UpdateProjection();
        
        DrawGizmosBasic();

        //DrawGizmos?.Invoke();
        Draw();

        DrawGizmoCameraOrbitCenter();
        DrawGizmoAxesWidget();
    }
    private void UpdateProjection () {
        float aspect = Window.Size.X/(float)Window.Size.Y;
        Projection = Matrix4X4.CreatePerspectiveFieldOfView(_cameraFOV, aspect, _cameraPlaneClose, _cameraPlaneFar);
        uView = Utils.MatrixToArray(View);
        uProjection = Utils.MatrixToArray(Projection);
    }

    private void DrawGizmoCameraOrbitCenter () {
        _shaderUnlit.Use();
        SetSceneUniforms(_shaderUnlit);
        float dist = (Camera.Instance.cameraOrbitCenterPos - Camera.Instance.cameraPos).Length;
        Matrix4X4<float> gizmoSphereModel = Matrix4X4.CreateScale(cameraOrbitCenterRadius*dist*0.01f)
            *Matrix4X4.CreateTranslation(Camera.Instance.cameraOrbitCenterPos);
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
    private void DrawGizmosBasic () {
        GL.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);

        /// Grid
        _shaderUnlit.Use();
        SetSceneUniforms(_shaderUnlit);
        _shaderUnlit.SetMatrix4("uModel", _uModelIdentity);
        _shaderUnlit.SetVector3("uColor", 0.35f, 0.35f, 0.4f);
        //_shaderUnlit.SetFloat("uAlpha", 1f);
        _grid.Draw();

        /// Axes
        GL.Disable(EnableCap.DepthTest);
        _shaderAxes.Use();
        SetSceneUniforms(_shaderAxes);
        _shaderAxes.SetMatrix4("uModel", _uModelIdentity);
        _axes.Draw();
        GL.Enable(EnableCap.DepthTest);
    }
    private void DrawGizmoAxesWidget () {
        const int gizmoSize = 90;
        const int gizmoMargin = 16;

        int windowWidth = Window.Size.X;
        int windowHeight = Window.Size.Y;

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

        GL.Viewport(Window.Size);
    }


    private void SetSceneUniforms (Shader shader) {
        shader.SetMatrix4("uView", uView);
        shader.SetMatrix4("uProjection", uProjection);
        shader.SetVector3("uSunLightColor", sunLightColor.X, sunLightColor.Y, sunLightColor.Z);
        shader.SetFloat("uSunLightIntensity", sunLightIntensity);
        shader.SetVector3("uSunLightDir", sunLightDir.X, sunLightDir.Y, sunLightDir.Z);
        shader.SetVector3("uViewPos", Camera.Instance.cameraPos.X, Camera.Instance.cameraPos.Y, Camera.Instance.cameraPos.Z);
    }


    internal void OnFramebufferResize (Vector2D<int> newSize) {
        GL.Viewport(newSize);
        if (0 < newSize.X && 0 < newSize.Y) UpdateProjection();
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
    }

}
