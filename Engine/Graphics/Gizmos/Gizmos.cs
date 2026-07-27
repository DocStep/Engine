using Silk.NET.OpenGL;
using Engine.Graphics;
using static Engine.AssetsEngine;
using static Engine.Graphics.Shader;

namespace Engine;


public static class Gizmos {
    static Gizmos () {
        //Renderer.Instance.de_GizmosDraw += DrawGizmos;
        GL = Renderer.GL;

        _sh_GizmoGrid = new Graphics.Shader(Assets.LoadText("src/Shaders/Grid_Vertex.shader"), Assets.LoadText("src/Shaders/Grid_Fragment.shader"), "Grid");
        _sh_GizmoAxes = new Graphics.Shader(Assets.LoadText("src/Shaders/Axes_Vertex.shader"), Assets.LoadText("src/Shaders/Axes_Fragment.shader"), "Axes");
        _sh_Outline = new Graphics.Shader(Assets.LoadText("src/Shaders/Outline_Vertex.shader"), Assets.LoadText("src/Shaders/Outline_Fragment.shader"), "Axes");

        _mat_GizmosGreen = new Material(_sh_Unlit);
        _mat_GizmosGreen.SetVector3(Color, Constants.green);
        _mat_GizmosGreen.SetFloat(Alpha, 0.5f);
        _mat_GizmosGreen.pass = RenderPass.Gizmo;
        _mat_GizmosGreen.face = RenderFace.Both;

        _mat_GizmoWireframe = new Material(_mat_GizmosGreen);
        _mat_GizmoWireframe.SetVector3(Color, Constants.black);
        _mat_GizmoWireframe.SetFloat(Alpha, 0.1f);

        _mat_GizmoGrid = new Material(_sh_GizmoGrid);
        _mat_GizmoGrid.SetVector3(Color, Constants.lightGray);
        _mat_GizmoGrid.SetFloat(Alpha, 0.5f);
        _mat_GizmoGrid.SetFloat(Radius, 200f);
        _mat_GizmoGrid.SetFloat(Fade, 50f);
        _mat_GizmoGrid.pass = RenderPass.Gizmo;
        _mat_GizmosGreen.face = RenderFace.Both;

        _mat_GizmoAxes = new Material(_sh_GizmoAxes);
        _mat_GizmoAxes.SetFloat(Alpha, 0.5f);
        _mat_GizmoAxes.SetFloat(Radius, 200f);
        _mat_GizmoAxes.SetFloat(Fade, 50f);
        _mat_GizmoAxes.pass = RenderPass.Gizmo;
        _mat_GizmosGreen.face = RenderFace.Both;

        _mat_GizmoSun = new Material(_sh_Unlit);
        _mat_GizmoSun.SetVector3(Color, Constants.yellow);
        _mat_GizmoSun.SetFloat(Alpha, 0.5f);
        _mat_GizmoSun.pass = RenderPass.Gizmo;
        _mat_GizmosGreen.face = RenderFace.Both;

        _mesh_CubeWireframe = new Mesh(Cube.GenerateWireframe());
        _mesh_SphereWireframe = new Mesh(Sphere.GenerateWireframe());
        _mesh_CapsuleWireframe = new Mesh(Capsule.GenerateWireframe());

        _mesh_PlaneWireframe = new Mesh(Graphics.Plane.GenerateWireframe());
        _mesh_GridWireframe = new Mesh(Graphics.Plane.GenerateWireframe(size: Constants._gridScale,
            divisions: (int)(Constants._gridScale*Constants._gridDivisionScale)));

        _mesh_AxesWireframe = new Mesh(Axes.GenerateWireframe(length: Constants._gridScale));
        _mesh_Arrow3D = new Mesh(Arrow.Generate(length: 1f, shaftWidth: 0.01f, headLength: 0.2f, headWidth: 0.1f));
        _mesh_ArrowWireframe = new Mesh(Arrow.GenerateWireframe(length: 1f, shaftWidth: 0.01f, headLength: 0.2f, headWidth: 0.1f));

        _gizmo_Selected = new GizmoSelected();

    }

    static GL GL = null!;

    public readonly static Mesh _mesh_CubeWireframe = null!;
    public readonly static Mesh _mesh_SphereWireframe = null!;
    public readonly static Mesh _mesh_CapsuleWireframe = null!;

    public readonly static Mesh _mesh_GridWireframe = null!;
    public readonly static Mesh _mesh_PlaneWireframe = null!;
    public readonly static Mesh _mesh_AxesWireframe = null!;
    public readonly static Mesh _mesh_Arrow3D = null!;
    public readonly static Mesh _mesh_ArrowWireframe = null!;

    public readonly static Graphics.Shader _sh_GizmoGrid = null!;
    public readonly static Graphics.Shader _sh_GizmoAxes = null!;
    public readonly static Graphics.Shader _sh_Outline = null!;

    public readonly static Material _mat_GizmoGrid = null!;
    public readonly static Material _mat_GizmoAxes = null!;
    public readonly static Material _mat_GizmosGreen = null!;
    public readonly static Material _mat_GizmoWireframe = null!;
    public readonly static Material _mat_GizmoSun = null!;

    public readonly static GizmoSelected _gizmo_Selected = null!;

    private static float cameraOrbitCenterRadius = 0.5f;


    public static void Update () {
        GizmoGrid();
        GizmoAxes();
        GizmoSun();

        GLDebug.DrawAll();

        //GizmoCameraOrbitCenter();
    }
    public static void Draw () {
        //Renderer.Instance.de_GizmosDraw?.Invoke();

        /// UI Layer — separate camera/viewport
        DrawGizmoAxesWidget();
    }
    private static void GizmoGrid () {
        _mat_GizmoGrid.SetVector3(CameraPos, Camera.Instance.cameraPos);
        Renderer.Instance.AddRenderInfo(new RenderInfo() {
            mesh = _mesh_GridWireframe,
            primitiveType = PrimitiveType.Lines,
            material = _mat_GizmoGrid,
            //depthRangeNear = 0.0001f,
            //de_Pre = GizmoGridPre,
            //de_Post = GizmoGridPost,
        });
    }
    static void GizmoGridPre () => GL.DepthRange(0.0001, 1);
    static void GizmoGridPost () => GL.DepthRange(0, 1);

    private static void GizmoAxes () {
        _mat_GizmoAxes.SetVector3(CameraPos, Camera.Instance.cameraPos);
        Renderer.Instance.AddRenderInfo(new RenderInfo() {
            mesh = _mesh_AxesWireframe,
            primitiveType = PrimitiveType.Lines,
            material = _mat_GizmoAxes,
            depthRangeFar = 0.9999f,
        });
    }
    private static void DrawGizmoAxesWidget () {
        const int gizmoSize = 90;
        const int gizmoMargin = 16;

        int windowWidth = Engine.Window.Size.X;
        int windowHeight = Engine.Window.Size.Y;

        int gizmoX = windowWidth - gizmoSize - gizmoMargin;
        int gizmoY = windowHeight - gizmoSize - gizmoMargin;

        GL.Disable(EnableCap.DepthTest);
        GL.Viewport(gizmoX, gizmoY, (uint)gizmoSize, (uint)gizmoSize);
        GL.Clear(ClearBufferMask.DepthBufferBit);

        Matrix4x4 rotation = Camera.Instance.cameraRot;
        Vector3 forward = Vector3.Transform(Vector3.UnitZ, rotation);
        Vector3 up = Vector3.Transform(Vector3.UnitY, rotation);
        Vector3 gizmoCamPos = -forward*5f;
        Matrix4x4 gizmoView = Matrix4x4.CreateLookAtLeftHanded(gizmoCamPos, Vector3.Zero, up);
        float aspect = Engine.Window.Size.X/(float)Engine.Window.Size.Y;
        Matrix4x4 gizmoProjection = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(
            Constants._cameraFOV, aspect, Constants._cameraPlaneClose, Constants._cameraPlaneFar);

        _sh_GizmoAxes.Use();
        _sh_GizmoAxes.SetMatrix4(View, Matrix4x4.ToArray(gizmoView));
        _sh_GizmoAxes.SetMatrix4(Projection, Matrix4x4.ToArray(gizmoProjection));
        _sh_GizmoAxes.SetMatrix4(Model, Matrix4x4.ToArray(Matrix4x4.CreateScale(0.002f)));

        _mesh_AxesWireframe.Draw(PrimitiveType.Lines);

        GL.Enable(EnableCap.DepthTest);
        GL.Viewport(Engine.Window.Size);
    }
    /*private static Material mat_GizmoCameraOrbitCenter = new Material() { Color = new Vector3(0.5f, 0.5f, 0.5f), Alpha = 0.2f, };
    private static void GizmoCameraOrbitCenter () {
        if (CameraEditor.Instance is null) return;

        float dist = (CameraEditor.Instance.cameraOrbitCenterPos - Camera.Instance.cameraPos).Length();

        Renderer.Instance.AddRenderInfo(new RenderInfo() {
            pos = CameraEditor.Instance.cameraOrbitCenterPos,
            scale = Vector3.One*cameraOrbitCenterRadius*dist*0.01f,
            mesh = _mesh_Sphere,
            shader = _sh_Unlit,
            pass = RenderPass.Gizmo,
            depthWrite = false,
            material = mat_GizmoCameraOrbitCenter,
        });
    }*/

    private static void GizmoSun () {
        if (!Constants.drawGizmosSun) return;
        Renderer.Instance.AddRenderInfo(new RenderInfo() {
            pos = new Vector3(0f, 5f, 0f),
            rot = Vector3.DirectionToEuler(Constants.sunLightDir),
            mesh = Constants._drawArrowAsMesh ? _mesh_Arrow3D : _mesh_ArrowWireframe,
            primitiveType = Constants._drawArrowAsMesh ? PrimitiveType.Triangles : PrimitiveType.Lines,
            material = _mat_GizmoSun,
        });
    }


    internal static void Dispose () {
        _mesh_CubeWireframe.Dispose();
        _mesh_SphereWireframe.Dispose();
        _mesh_CapsuleWireframe.Dispose();

        _mesh_GridWireframe.Dispose();
        _mesh_PlaneWireframe.Dispose();
        _mesh_AxesWireframe.Dispose();
        _mesh_Arrow3D.Dispose();
        _mesh_ArrowWireframe.Dispose();

        _sh_GizmoGrid.Dispose();
        _sh_GizmoAxes.Dispose();
        _sh_Outline.Dispose();

        //_mat_GizmoGrid.Dispose();
        //_mat_Axes.Dispose();
        //_mat_GizmoAxes.Dispose();
        //_mat_GizmosG.Dispose();
        //_mat_GizmoSun.Dispose();
    }

}
