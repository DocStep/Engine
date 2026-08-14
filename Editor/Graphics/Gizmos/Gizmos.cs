using Silk.NET.OpenGL;
using Engine.Graphics;
using static Engine.AssetsEngine;
using static Engine.Graphics.Shader;
using Shader = Engine.Graphics.Shader;

namespace Editor.Graphics;


public static class Gizmos {
    static Gizmos () {
        //Renderer.Instance.de_GizmosDraw += DrawGizmos;
        GL = RendererEditor.GL;

        _sh_Outline = new Shader(Assets.LoadText("src/Shaders/Outline_Vertex.shader"), Assets.LoadText("src/Shaders/Outline_Fragment.shader"), "Outline");
        _sh_Outline.SetVector3(Color, Constants.cyan);
        
        //_sh_DepthClear = new Graphics.Shader(Assets.LoadText("src/Shaders/DepthClear_Vertex.shader"), Assets.LoadText("src/Shaders/DepthClear_Fragment.shader"), "DepthClear");
        
        _mat_GizmosGreen = new Material(_sh_Unlit);
        _mat_GizmosGreen.SetVector3(Color, Constants.green);
        _mat_GizmosGreen.SetFloat(Alpha, 0.5f);
        _mat_GizmosGreen.pass = RenderPass.Transparent;
        _mat_GizmosGreen.face = RenderFace.Both;
        _mat_GizmosGreen.depthWrite = false;

        _mat_GizmoWireframe = new Material(_sh_Unlit);
        _mat_GizmoWireframe.SetVector3(Color, Constants.black);
        _mat_GizmoWireframe.SetFloat(Alpha, 0.1f);
        _mat_GizmoWireframe.pass = RenderPass.Transparent;
        _mat_GizmoWireframe.face = RenderFace.Both;
        _mat_GizmoWireframe.depthWrite = false;

        _sh_GizmoGrid = new Shader(Assets.LoadText("src/Shaders/Grid_Vertex.shader"), Assets.LoadText("src/Shaders/Grid_Fragment.shader"), "Grid");
        _mat_GizmoGrid = new Material(_sh_GizmoGrid);
        _mat_GizmoGrid.SetVector3(Color, Constants.lightGray);
        _mat_GizmoGrid.SetFloat(Alpha, 0.2f);
        _mat_GizmoGrid.SetFloat(Radius, 100f);
        _mat_GizmoGrid.SetFloat(Fade, 10f);
        _mat_GizmoGrid.pass = RenderPass.Transparent;
        _mat_GizmoGrid.face = RenderFace.Both;
        _mat_GizmoGrid.depthWrite = false;

        _sh_GizmoAxisLine = new Shader(Assets.LoadText("src/Shaders/AxisLine_Vertex.shader"), Assets.LoadText("src/Shaders/AxisLine_Fragment.shader"), "AxisLine");
        _mat_GizmoAxisLine = new Material(_sh_GizmoAxisLine);
        _mat_GizmoAxisLine.SetFloat(Alpha, 0.5f);
        _mat_GizmoAxisLine.SetFloat(Radius, 100f);
        _mat_GizmoAxisLine.SetFloat(Fade, 10f);
        _mat_GizmoAxisLine.pass = RenderPass.Transparent;
        _mat_GizmoAxisLine.face = RenderFace.Both;
        _mat_GizmoAxisLine.depthWrite = false;

        _sh_GizmoAxis = new Shader(Assets.LoadText("src/Shaders/Axis_Vertex.shader"), Assets.LoadText("src/Shaders/Axis_Fragment.shader"), "Axis");
        _mat_GizmoAxis = new Material(_sh_GizmoAxis);
        _mat_GizmoAxis.SetFloat(Alpha, 0.5f);
        _mat_GizmoAxis.SetFloat(Radius, 100f);
        _mat_GizmoAxis.SetFloat(Fade, 10f);
        _mat_GizmoAxis.pass = RenderPass.Transparent;
        _mat_GizmoAxis.face = RenderFace.Both;
        _mat_GizmoAxis.depthWrite = false;

        _mat_GizmoSun = new Material(_sh_Unlit);
        _mat_GizmoSun.SetVector3(Color, Constants.yellow);
        _mat_GizmoSun.SetFloat(Alpha, 0.5f);
        _mat_GizmoSun.pass = RenderPass.Transparent;
        _mat_GizmoSun.face = RenderFace.Both;
        _mat_GizmoSun.depthWrite = false;

        _mesh_CubeWireframe = new Mesh(Cube.GenerateWireframe());
        _mesh_SphereWireframe = new Mesh(Sphere.GenerateWireframe());
        _mesh_CapsuleWireframe = new Mesh(Capsule.GenerateWireframe());

        _mesh_GridWireframe = new Mesh(Engine.Graphics.Plane.GenerateWireframe(size: Constants._gridScale,
            divisions: (int)(Constants._gridScale*Constants._gridDivisionScale)));
        _mesh_PlaneWireframe = new Mesh(Engine.Graphics.Plane.GenerateWireframe());

        _mesh_Line = new Mesh(Line.GenerateWireframe());
        _mesh_AxesWireframe = new Mesh(Axes.GenerateWireframe());
        _mesh_Arrow3D = new Mesh(Arrow.Generate(length: 1f, shaftWidth: 0.01f, headLength: 0.2f, headWidth: 0.1f));
        _mesh_ArrowWireframe = new Mesh(Arrow.GenerateWireframe(length: 1f, shaftWidth: 0.01f, headLength: 0.2f, headWidth: 0.1f));

        _gizmo_Selected = new GizmoSelected();

    }

    static GL GL = null!;

    public readonly static Mesh _mesh_CubeWireframe = null!;
    public readonly static Mesh _mesh_SphereWireframe = null!;
    public readonly static Mesh _mesh_CapsuleWireframe = null!;

    public readonly static Mesh _mesh_Line = null!;
    public readonly static Mesh _mesh_GridWireframe = null!;
    public readonly static Mesh _mesh_PlaneWireframe = null!;
    public readonly static Mesh _mesh_AxesWireframe = null!;
    public readonly static Mesh _mesh_Arrow3D = null!;
    public readonly static Mesh _mesh_ArrowWireframe = null!;

    public readonly static Shader _sh_GizmoGrid = null!;
    public readonly static Shader _sh_GizmoAxis = null!;
    public readonly static Shader _sh_GizmoAxisLine = null!;
    public readonly static Shader _sh_Outline = null!;
    //public readonly static Graphics.Shader _sh_DepthClear = null!;

    public readonly static Material _mat_GizmoGrid = null!;
    public readonly static Material _mat_GizmoAxis = null!;
    public readonly static Material _mat_GizmoAxisLine = null!;
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
        _gizmo_Selected.Update();
    }
    public static void Draw () {
        //Renderer.Instance.de_GizmosDraw?.Invoke();

        /// UI Layer — separate camera/viewport
        DrawGizmoAxesWidget();
    }
    private static void GizmoGrid () {
        Vector3 pos = Camera.Current.CameraPos;
        _mat_GizmoGrid.SetVector3(CameraPos, pos);

        Renderer.Instance.AddRenderInfo(new RenderInfo() {
            model = Matrix4x4.CreateTranslation(new Vector3(pos.X - pos.X%(1f/Constants._gridDivisionScale), 
                0, pos.Z - pos.Z%(1f/Constants._gridDivisionScale))),
            mesh = _mesh_GridWireframe,
            primitiveType = PrimitiveType.Lines,
            material = _mat_GizmoGrid,
        });
    }
    static void GizmoGridPre () => GL.DepthRange(0.0001, 1);
    static void GizmoGridPost () => GL.DepthRange(0, 1);

    private static void GizmoAxes () {
        Vector3 pos = Camera.Current.CameraPos;
        float halfPi = MathF.PI/2f;

        _mat_GizmoAxisLine.SetVector3(CameraPos, pos);

        /// X Red
        RenderInfo info = new RenderInfo() {
            model = Matrix4x4.CreateScale(Constants._gridScale*Vector3.One)*Matrix4x4.CreateRotationY(halfPi),
            mesh = _mesh_Line,
            primitiveType = PrimitiveType.Lines,
            material = _mat_GizmoAxisLine,
            depthRangeFar = 0.9999f,
        };
        Renderer.Instance.AddRenderInfo(info);

        /// Y Green
        info.model = Matrix4x4.CreateScale(Constants._gridScale*Vector3.One)*Matrix4x4.CreateRotationX(-halfPi);
        Renderer.Instance.AddRenderInfo(info);

        /// Z Blue
        info.model = Matrix4x4.CreateScale(Constants._gridScale*Vector3.One);
        Renderer.Instance.AddRenderInfo(info);
    }
    private static void DrawGizmoAxesWidget () {
        const int gizmoSize = 90;
        const int gizmoMargin = 16;
            
        int windowWidth = Windows.Window.Size.X;
        int windowHeight = Windows.Window.Size.Y;

        int gizmoX = windowWidth - gizmoSize - gizmoMargin;
        int gizmoY = windowHeight - gizmoSize - gizmoMargin;

        GL.Disable(EnableCap.DepthTest);
        GL.Viewport(gizmoX, gizmoY, (uint)gizmoSize, (uint)gizmoSize);

        GL.Enable(EnableCap.ScissorTest);
        GL.Scissor(gizmoX, gizmoY, (uint)gizmoSize, (uint)gizmoSize);
        GL.Clear(ClearBufferMask.DepthBufferBit);
        GL.Disable(EnableCap.ScissorTest);

        Matrix4x4 rotation = Camera.Current.GetRotationMatrix();
        Vector3 forward = Vector3.Transform(Vector3.UnitZ, rotation);
        Vector3 up = Vector3.Transform(Vector3.UnitY, rotation);
        Vector3 gizmoCamPos = -forward*5f;
        Matrix4x4 gizmoView = Matrix4x4.CreateLookAtLeftHanded(gizmoCamPos, Vector3.Zero, up);
        float aspect = Windows.Window.Size.X/(float)Windows.Window.Size.Y;
        Matrix4x4 gizmoProjection = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(
            Camera.Current.FOV/180*MathF.PI, aspect, Camera.Current.planeNear, Camera.Current.planeFar);

        _sh_GizmoAxis.Use();
        _sh_GizmoAxis.SetMatrix4x4(View, gizmoView);
        _sh_GizmoAxis.SetMatrix4x4(Projection, gizmoProjection);
        _sh_GizmoAxis.SetMatrix4x4(Model, Matrix4x4.CreateScale(0.002f));

        _mesh_AxesWireframe.Draw(PrimitiveType.Lines);

        GL.Enable(EnableCap.DepthTest);
        GL.Viewport(Windows.Window.Size);
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
            model = Matrix4x4.CreateScale(Vector3.One)*Matrix4x4.RotationFromDirection(Constants.sunLightDir)
                *Matrix4x4.CreateTranslation(new Vector3(0f, 5f, 0f)),
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
        _sh_GizmoAxis.Dispose();
        _sh_Outline.Dispose();

        //_mat_GizmoGrid.Dispose();
        //_mat_Axes.Dispose();
        //_mat_GizmoAxes.Dispose();
        //_mat_GizmosG.Dispose();
        //_mat_GizmoSun.Dispose();
    }

}
