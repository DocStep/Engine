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

        _mat_Default = new Material { Color = new(0.6f, 0.55f, 0.5f), Roughness = 0.5f };
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


    private IWindow Window = null!;
    internal GL GL = null!;

    internal Shader _shader = null!;
    internal Shader _shaderUnlit = null!;
    internal Shader _shaderAxes = null!;

    internal Material _mat_Default = null!;
    internal Material _mat_DefaultUnlit = null!;
    internal Material _mat_DefaultAxes = null!;

    private Cube _cube = null!;
    private Sphere _sphere = null!;
    private Sphere _gizmoSphere = null!;
    private WorldGrid _grid = null!;
    private WorldAxes _axes = null!;
    private WorldAxes _gizmoAxes = null!;

    internal Vector3D<float> lightGray = new Vector3D<float>(0.78f, 0.78f, 0.78f);
    internal Vector3D<float> lightDir = Vector3D.Normalize(new Vector3D<float>(-0.4f, -1f, -0.3f));
    //internal Vector3D<float> lightDir = Vector3D.Normalize(new Vector3D<float>(0f, -1f, 0f));
    internal Vector3D<float> lightColor = new Vector3D<float>(1f, 1f, 1f);

    /// Debug
    internal Matrix4X4<float> View;
    internal Matrix4X4<float> Projection;



    private void UpdateProjection () {
        float aspect = Window.Size.X/(float)Window.Size.Y;
        Projection = Matrix4X4.CreatePerspectiveFieldOfView(Camera._cameraFOV, aspect, Camera._cameraPlaneClose, Camera._cameraPlaneFar);
    }

    private void LookAtOrbitCenter () {
        Vector3D<float> offset = Camera.Instance.cameraPos - Camera.Instance.cameraOrbitCenterPos;
        float dist = offset.Length;
        if (dist < 0.0001f) return;

        Vector3D<float> forward = -offset / dist;

        Camera.Instance.pitch = MathF.Asin(Utils.Clamp(forward.Y, -1f, 1f));
        float cosPitch = MathF.Cos(Camera.Instance.pitch);
        Camera.Instance.yaw = MathF.Atan2(-forward.X / cosPitch, -forward.Z / cosPitch);

        Camera.Instance.cameraRot = Utils.CreateFromYawPitchRoll(Camera.Instance.yaw, Camera.Instance.pitch, 0f);
    }


    internal void OnRender (double deltaTime) {
        GL.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);

        Matrix4X4<float> modelIdentity = Matrix4X4<float>.Identity;

        /// Grid (unlit, opaque, gray)
        _shaderUnlit.Use();
        _shaderUnlit.SetMatrix4("uModel", Utils.MatrixToArray(modelIdentity));
        _shaderUnlit.SetMatrix4("uView", Utils.MatrixToArray(View));
        _shaderUnlit.SetMatrix4("uProjection", Utils.MatrixToArray(Projection));
        _shaderUnlit.SetVector3("uColor", 0.35f, 0.35f, 0.4f);
        _shaderUnlit.SetFloat("uAlpha", 1f);
        _grid.Draw();

        /// Axes (vertex-colored, unlit, always on top of grid/scene)
        GL.Disable(EnableCap.DepthTest);
        _shaderAxes.Use();
        _shaderAxes.SetMatrix4("uModel", Utils.MatrixToArray(modelIdentity));
        _shaderAxes.SetMatrix4("uView", Utils.MatrixToArray(View));
        _shaderAxes.SetMatrix4("uProjection", Utils.MatrixToArray(Projection));
        _axes.Draw();
        GL.Enable(EnableCap.DepthTest);


        /// Cube (lit, opaque, light gray)
        _shader.Use();
        SetSceneUniforms(_shader);

        Matrix4X4<float> cubeModel =
            //Matrix4X4.CreateRotationX(MathF.PI / 4f) *
            //Matrix4X4.CreateRotationY(MathF.PI / 4f) *
            Matrix4X4.CreateTranslation(new Vector3D<float>(0f, 0f, 0f));
        _shader.SetMatrix4("uModel", Utils.MatrixToArray(cubeModel));
        _shader.SetVector3("uColor", lightGray.X, lightGray.Y, lightGray.Z);
        _cube.Draw();

        Matrix4X4<float> sphereModel = Matrix4X4.CreateScale(0.5f)*Matrix4X4.CreateTranslation(new Vector3D<float>(1.5f, 0f, 0f));
        _shader.SetMatrix4("uModel", Utils.MatrixToArray(sphereModel));
        _sphere.Draw();

        /// Sphere (unlit, semi-transparent, small pivot marker, centered on orbit center)
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.DepthMask(false);

        _shaderUnlit.Use();
        Matrix4X4<float> gizmoSphereModel = Matrix4X4.CreateScale(0.05f)*Matrix4X4.CreateTranslation(Camera.Instance.cameraOrbitCenterPos);
        _shaderUnlit.SetMatrix4("uModel", Utils.MatrixToArray(gizmoSphereModel));
        _shaderUnlit.SetMatrix4("uView", Utils.MatrixToArray(View));
        _shaderUnlit.SetMatrix4("uProjection", Utils.MatrixToArray(Projection));
        _shaderUnlit.SetVector3("uColor", 0f, 0f, 0f);
        _shaderUnlit.SetFloat("uAlpha", 0.2f);
        _gizmoSphere.Draw();

        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);

        DrawGizmo();
        
        UpdateProjection();
    }


    private void DrawGizmo () {
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
        shader.SetMatrix4("uView", Utils.MatrixToArray(View));
        shader.SetMatrix4("uProjection", Utils.MatrixToArray(Projection));
        shader.SetVector3("uLightDir", lightDir.X, lightDir.Y, lightDir.Z);
        shader.SetVector3("uLightColor", lightColor.X, lightColor.Y, lightColor.Z);
        shader.SetVector3("uViewPos", Camera.Instance.cameraPos.X, Camera.Instance.cameraPos.Y, Camera.Instance.cameraPos.Z);
    }


    internal void OnFramebufferResize (Vector2D<int> newSize) {
        GL.Viewport(newSize);
        if (newSize.X > 0 && newSize.Y > 0)
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
    }

}
