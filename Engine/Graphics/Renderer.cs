using System.Numerics;
using Silk.NET.OpenGL;
using Engine.Graphics.UI;
using Engine.Input;
using static Engine.Graphics.Shader;

namespace Engine.Graphics;


public class Renderer {
    public Renderer () {
        Instance = this;

        Engine.Window.Render += OnRender;
        Engine.Window.Closing += OnClosing;
        Engine.Window.FramebufferResize += OnFrameBufferResize;

        GL = Engine.Window.CreateOpenGL();
        GL.FrontFace(FrontFaceDirection.CW);
        GL.ClearColor(0.1f, 0.1f, 0.15f, 1f);

        _mesh_Cube = new Mesh(Cube.Generate());
        _mesh_CubeWireframe = new Mesh(Cube.GenerateWireframe());
        _mesh_Sphere = new Mesh(Sphere.Generate());
        _mesh_SphereWireframe = new Mesh(Sphere.GenerateWireframe());
        _mesh_Capsule = new Mesh(Capsule.Generate());
        _mesh_CapsuleWireframe = new Mesh(Capsule.GenerateWireframe());
        
        _mesh_Plane = new Mesh(Plane.Generate());
        _mesh_PlaneWireframe = new Mesh(Plane.GenerateWireframe());
        _mesh_PlaneQuad = new Mesh(Plane.Generate(divisions: 1));
        _mesh_GridWireframe = new Mesh(Plane.GenerateWireframe(size: Constants._gridScale, 
            divisions: (int)(Constants._gridScale*Constants._gridDivisionScale)));

        _mesh_AxesWireframe = new Mesh(Axes.GenerateWireframe(length: Constants._gridScale));
        _mesh_Arrow3D = new Mesh(Arrow.Generate(length: 1f, shaftWidth: 0.01f, headLength: 0.2f, headWidth: 0.1f));
        _mesh_ArrowWireframe = new Mesh(Arrow.GenerateWireframe(length: 1f, shaftWidth: 0.01f, headLength: 0.2f, headWidth: 0.1f));

        _sh_Lit = new Shader(Utils.LoadSrc("src/Shaders/Lit_Vertex.shader"), Utils.LoadSrc("src/Shaders/Lit_Fragment.shader"), "Lit");
        _sh_Unlit = new Shader(Utils.LoadSrc("src/Shaders/Unlit_Vertex.shader"), Utils.LoadSrc("src/Shaders/Unlit_Fragment.shader"), "Unlit");
        _sh_Grid = new Shader(Utils.LoadSrc("src/Shaders/Grid_Vertex.shader"), Utils.LoadSrc("src/Shaders/Grid_Fragment.shader"), "Grid");
        _sh_Axes = new Shader(Utils.LoadSrc("src/Shaders/Axes_Vertex.shader"), Utils.LoadSrc("src/Shaders/Axes_Fragment.shader"), "Axes");
        _sh_Outline = new Shader(Utils.LoadSrc("src/Shaders/Outline_Vertex.shader"), Utils.LoadSrc("src/Shaders/Outline_Fragment.shader"), "Axes");

        _sh_Skybox = new Shader(Utils.LoadSrc("src/Shaders/Skybox_Vertex.shader"), Utils.LoadSrc("src/Shaders/Skybox_Fragment.shader"), "Skybox");
        //_hdrTexture = new HdrTexture("src/hdr/autumn_field_puresky_4k.hdr");
        _hdrTexture_Skybox = new HdrTexture("src/hdr/rogland_clear_night_4k.hdr");
        ///_hdrTexture = new HdrTexture("src/hdr/grasslands_sunset_4k.hdr");
        ///_hdrTexture = new HdrTexture("src/hdr/overcast_soil_puresky_4k.hdr");
        //_hdrTexture = new HdrTexture("src/hdr/qwantani_dusk_2_puresky_4k.hdr");
        _skybox = new Skybox(_sh_Skybox, _hdrTexture_Skybox);
        _skybox.BlurScale = 3f;

        _mat_Lit = new Material { Color = Constants.lightGray, };
        _mat_Smooth = new Material { Color = Constants.lightGray, Roughness = 0f, };
        _mat_Matt = new Material { Color = Constants.lightGray, Roughness = 1f, };
        _mat_Metallic = new Material { Color = Constants.gray, Roughness = 0.05f, Metallic = 1, };
        _mat_MaterialPreview = new Material { Color = Constants.white, Roughness = 0, Metallic = 1 };

        _mat_Unlit = new Material { Color = Constants.gray, Roughness = 1f, };

        _mat_Axes = new Material { Color = Constants.gray, };
        _mat_GizmosG = new Material { Color = Constants.green, Alpha = 0.5f, };

        _mat_LitWhite = new Material { Color = Vector3.One, };
        _mat_LitBlack = new Material { Color = Vector3.Zero, };
        _mat_LitGray = new Material { Color = 0.5f*Vector3.One, };
        _mat_LitRed = new Material { Color = new(1, 0, 0), };
        _mat_LitGreen = new Material { Color = new(0, 1, 0), };
        _mat_LitBlue = new Material { Color = new(0, 0, 1), };

        _mesh_Torus = new Mesh(ObjLoader.Load("src/Models/Torus.obj"));
        _mesh_Suzanne = new Mesh(ObjLoader.Load("src/Models/Suzanne.obj"));
        _mesh_SuzanneHighRes = new Mesh(ObjLoader.Load("src/Models/SuzanneHighRes.obj"));

        _gizmo_Selected = new GizmoSelected();

        TextRenderer = new TextRenderer();
    }
    public static Renderer Instance = null!;

    public readonly GL GL = null!;
    public readonly TextRenderer TextRenderer = null!;

    public Action? de_DrawGizmos = null;


    public readonly Shader _sh_Lit = null!;
    public readonly Shader _sh_Unlit = null!;
    public readonly Shader _sh_Grid = null!;
    public readonly Shader _sh_Axes = null!;
    public readonly Shader _sh_Outline = null!;

    public readonly Shader _sh_Skybox = null!;
    public readonly Skybox _skybox = null!;
    public readonly HdrTexture? _hdrTexture_Skybox = null;

    public readonly Material _mat_Axes = null!;
    public readonly Material _mat_GizmosG = null!;

    public readonly Material _mat_Lit = null!;
    public readonly Material _mat_Unlit = null!;
    public readonly Material _mat_Smooth = null!;
    public readonly Material _mat_Matt = null!;
    public readonly Material _mat_Metallic = null!;
    public readonly Material _mat_MaterialPreview = null!;

    public readonly Material _mat_LitWhite = null!;
    public readonly Material _mat_LitBlack = null!;
    public readonly Material _mat_LitGray = null!;
    public readonly Material _mat_LitRed = null!;
    public readonly Material _mat_LitGreen = null!;
    public readonly Material _mat_LitBlue = null!;

    public readonly Mesh _mesh_Cube = null!;
    public readonly Mesh _mesh_CubeWireframe = null!;
    public readonly Mesh _mesh_Sphere = null!;
    public readonly Mesh _mesh_SphereWireframe = null!;
    public readonly Mesh _mesh_Capsule = null!;
    public readonly Mesh _mesh_CapsuleWireframe = null!;

    public readonly Mesh _mesh_Plane = null!;
    public readonly Mesh _mesh_PlaneWireframe = null!;
    public readonly Mesh _mesh_PlaneQuad = null!;
    public readonly Mesh _mesh_GridWireframe = null!;

    public readonly Mesh _mesh_Torus = null!;
    public readonly Mesh _mesh_Suzanne = null!;
    public readonly Mesh _mesh_SuzanneHighRes = null!;

    public readonly Mesh _mesh_AxesWireframe = null!;
    public readonly Mesh _mesh_Arrow3D = null!;
    public readonly Mesh _mesh_ArrowWireframe = null!;

    public readonly GizmoSelected _gizmo_Selected = null!;
    private readonly static Matrix4x4 _modelIdentity = Matrix4x4.Identity;
    private readonly static float[] _uModelIdentity = Matrix4x4.ToArray(_modelIdentity);
    
    /// Gizmos
    private float cameraOrbitCenterRadius = 0.5f;

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



    private void SetSceneUniforms (Shader shader) {
        shader.Use();
        shader.SetMatrix4(View, uView);
        shader.SetMatrix4(Projection, uProjection);
        shader.SetVector3(ViewPos, Camera.Instance.cameraPos);
        shader.SetVector3(SunLightColor, Constants.sunLightColor);
        shader.SetVector3(SunLightDir, Constants.sunLightDir);
        shader.SetVector3(AmbientColor, 0.05f, 0.05f, 0.06f);
        shader.SetFloat(SunLightIntensity, Constants.sunLightIntensity);
        shader.SetFloat(ReflectionIntensity, Constants.reflectionIntensity);
        //shader.SetFloat("uExposure", 1f);

        if (_hdrTexture_Skybox is not null) {
            _hdrTexture_Skybox.Bind(TextureUnit.Texture0);
            shader.SetInt(Shader.Skybox, 0);

            float maxLod = MathF.Log2(MathF.Max(_hdrTexture_Skybox.Width, _hdrTexture_Skybox.Height));
            shader.SetFloat(MaxReflectionLod, maxLod);
        }
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

        SetSceneUniforms(renderInfo.shader);
        renderInfo.shader.SetMatrix4(Model, mesh_uModel);
        renderInfo.material.Apply(renderInfo.shader);
        renderInfo.mesh.Draw(renderInfo.primitiveType);
    }


    private void OnRender (double deltaTime) {
        UpdateProjection();

        GL.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit));
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);

        if (Constants.renderSkybox) _skybox?.Draw(m4x4_View, m4x4Projection);

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
        if (Constants.drawGizmos) DrawGizmos();

        /// Draw UI
        TextRenderer.DrawUI();

        DrawEnd();
    }
    private void UpdateProjection () {
        float aspect = Engine.Window.Size.X/(float)Engine.Window.Size.Y;
        m4x4Projection = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(Constants._cameraFOV, aspect, Constants._cameraPlaneClose, Constants._cameraPlaneFar);
        uView = Matrix4x4.ToArray(m4x4_View);
        uProjection = Matrix4x4.ToArray(m4x4Projection);
    }

    private void DrawGizmos () {
        DrawGizmoGrid();
        DrawGizmoAxes();
        if (Constants.drawGizmosSun) DrawGizmoSun();

        DrawGizmoCameraOrbitCenter();

        _gizmo_Selected.Draw();


        /// UI Layer
        DrawGizmoAxesWidget();
    }
    private void DrawGizmoGrid () {
        GL.DepthMask(false);

        GL.DepthRange(0.0001, 1.0);

        _sh_Grid.Use();
        SetSceneUniforms(_sh_Grid);
        _sh_Grid.SetMatrix4(Model, _uModelIdentity);
        _sh_Grid.SetVector3(CameraPos, Camera.Instance.cameraPos);
        _sh_Grid.SetVector3(Color, Constants.lightGray);
        _sh_Grid.SetFloat(Alpha, 0.5f);
        _sh_Grid.SetFloat(Radius, 200f);
        _sh_Grid.SetFloat(Fade, 50f);
        _mesh_GridWireframe.Draw(PrimitiveType.Lines);

        GL.DepthRange(0.0, 1.0);
        GL.DepthMask(true);
    }
    private void DrawGizmoAxes () {
        _sh_Axes.Use();
        SetSceneUniforms(_sh_Axes);
        _sh_Axes.SetMatrix4(Model, _uModelIdentity);
        _sh_Axes.SetVector3(CameraPos, Camera.Instance.cameraPos);
        _sh_Axes.SetFloat(Alpha, 0.5f);
        _sh_Axes.SetFloat(Radius, 200f);
        _sh_Axes.SetFloat(Fade, 50f);
        _mesh_AxesWireframe.Draw(PrimitiveType.Lines);
    }
    private void DrawGizmoAxesWidget () {
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

        _sh_Axes.Use();
        _sh_Axes.SetMatrix4(View, Matrix4x4.ToArray(gizmoView));
        _sh_Axes.SetMatrix4(Projection, Matrix4x4.ToArray(gizmoProjection));
        _sh_Axes.SetMatrix4(Model, Matrix4x4.ToArray(Matrix4x4.Identity));

        _mesh_AxesWireframe.Draw(PrimitiveType.Lines);

        GL.Enable(EnableCap.DepthTest);
        GL.Viewport(Engine.Window.Size);
    }
    private void DrawGizmoCameraOrbitCenter () {
        if (CameraEditor.Instance is null) return;

        GL.DepthMask(false);

        _sh_Unlit.Use();
        SetSceneUniforms(_sh_Unlit);
        float dist = (CameraEditor.Instance.cameraOrbitCenterPos - Camera.Instance.cameraPos).Length();
        Matrix4x4 gizmoSphereModel = Matrix4x4.CreateScale(cameraOrbitCenterRadius*dist*0.01f)
            *Matrix4x4.CreateTranslation(CameraEditor.Instance.cameraOrbitCenterPos);
        _sh_Unlit.SetMatrix4(Model, Matrix4x4.ToArray(gizmoSphereModel));
        _sh_Unlit.SetVector3(Color, 0.5f, 0.5f, 0.5f);
        _sh_Unlit.SetFloat(Alpha, 0.2f);
        _mesh_Sphere.Draw();

        GL.DepthMask(true);
    }
    private void DrawGizmoSun () {
        GL.Disable(EnableCap.CullFace);

        _sh_Unlit.Use();
        SetSceneUniforms(_sh_Unlit);
        //Matrix4x4 mesh_m4x4 = Utils.RotationFromDirection(Constants.sunLightDir)*Matrix4x4.Position(0f, 5f, 0f);
        Matrix4x4 mesh_m4x4 = Matrix4x4.RotationFromDirection(Constants.sunLightDir)*Matrix4x4.Position(0f, 5f, 0f);
        float[] mesh_uModel = Matrix4x4.ToArray(mesh_m4x4);
        _sh_Unlit.SetMatrix4(Model, mesh_uModel);
        _sh_Unlit.SetColor(Color, Constants.yellow);
        _sh_Unlit.SetFloat(Alpha, 0.5f);
        if (Constants._drawArrowAsMesh) _mesh_Arrow3D.Draw();
        else _mesh_ArrowWireframe.Draw(PrimitiveType.Lines);

        GL.Enable(EnableCap.CullFace);
    }




    

    private void DrawEnd () {
        RenderList.Clear();
    }



    public void DrawMaterialsGrid (float offsetX, float offsetZ, int testGridCount, float testGridDensity) {
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
        SetSceneUniforms(_sh_Lit);
        _sh_Lit.SetColor(Color, Constants.black);
        for (int x = 0; x < testGridCount*testGridDensity; x++) {
            for (int z = 0; z < testGridCount*testGridDensity; z++) {
                RenderList.Add(new() {
                    pos = new Vector3(2f*x/testGridDensity + offsetX, 0f, -2f*z/testGridDensity + offsetZ),
                    mesh = _mesh_Sphere,
                    shader = _sh_Lit,
                    material = _mat_Smooth,
                });

                Matrix4x4 mesh_m4x4 = Matrix4x4.CreateTranslation(
                    new Vector3(2f*x/testGridDensity + offsetX, 0f, -2f*z/testGridDensity + offsetZ));
                float[] mesh_uModel = Matrix4x4.ToArray(mesh_m4x4);
                _sh_Lit.SetMatrix4(Model, mesh_uModel);
                _sh_Lit.SetFloat(Roughness, 1f - x/testGridDensity/testGridCount);
                _sh_Lit.SetFloat(Metallic, z/testGridDensity/testGridCount);
                _mesh_Sphere.Draw();
            }
        }
    }



    internal void OnFrameBufferResize (Silk.NET.Maths.Vector2D<int> newSize) {
        GL.Viewport(newSize);
        if (0 < newSize.X && 0 < newSize.Y) 
            UpdateProjection();
    }

    internal void OnClosing () {
        _mesh_Cube.Dispose();
        _mesh_Sphere.Dispose();
        //_GizmoGrid.Dispose();
        //_GizmoAxes.Dispose();
        //_GizmoAxesWidget.Dispose();
        _mesh_Arrow3D.Dispose();
        _sh_Lit.Dispose();
        _sh_Unlit.Dispose();
        _sh_Grid.Dispose();
        _sh_Axes.Dispose();
        _skybox.Dispose();
        _hdrTexture_Skybox?.Dispose();
        _sh_Skybox.Dispose();
        TextRenderer.Dispose();
    }

}
