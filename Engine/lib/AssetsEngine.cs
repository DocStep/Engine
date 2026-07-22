using Engine.Graphics;

namespace Engine;


public class AssetsEngine : Singleton<AssetsEngine> {
    static AssetsEngine () {
        Engine.Window.Closing += OnClosing;

        _mesh_Cube = new Mesh(Cube.Generate());
        _mesh_CubeWireframe = new Mesh(Cube.GenerateWireframe());
        _mesh_Sphere = new Mesh(Sphere.Generate());
        _mesh_SphereWireframe = new Mesh(Sphere.GenerateWireframe());
        _mesh_Capsule = new Mesh(Capsule.Generate());
        _mesh_CapsuleWireframe = new Mesh(Capsule.GenerateWireframe());

        _mesh_Plane = new Mesh(Graphics.Plane.Generate());
        _mesh_PlaneWireframe = new Mesh(Graphics.Plane.GenerateWireframe());
        _mesh_PlaneQuad = new Mesh(Graphics.Plane.Generate(divisions: 1));
        _mesh_GridWireframe = new Mesh(Graphics.Plane.GenerateWireframe(size: Constants._gridScale,
            divisions: (int)(Constants._gridScale*Constants._gridDivisionScale)));

        _mesh_AxesWireframe = new Mesh(Axes.GenerateWireframe(length: Constants._gridScale));
        _mesh_Arrow3D = new Mesh(Arrow.Generate(length: 1f, shaftWidth: 0.01f, headLength: 0.2f, headWidth: 0.1f));
        _mesh_ArrowWireframe = new Mesh(Arrow.GenerateWireframe(length: 1f, shaftWidth: 0.01f, headLength: 0.2f, headWidth: 0.1f));

        _sh_Lit = new Shader(Utils.LoadTextFile("src/Shaders/Lit_Vertex.shader"), Utils.LoadTextFile("src/Shaders/Lit_Fragment.shader"), "Lit");
        _sh_Unlit = new Shader(Utils.LoadTextFile("src/Shaders/Unlit_Vertex.shader"), Utils.LoadTextFile("src/Shaders/Unlit_Fragment.shader"), "Unlit");
        _sh_Grid = new Shader(Utils.LoadTextFile("src/Shaders/Grid_Vertex.shader"), Utils.LoadTextFile("src/Shaders/Grid_Fragment.shader"), "Grid");
        _sh_Axes = new Shader(Utils.LoadTextFile("src/Shaders/Axes_Vertex.shader"), Utils.LoadTextFile("src/Shaders/Axes_Fragment.shader"), "Axes");
        _sh_Outline = new Shader(Utils.LoadTextFile("src/Shaders/Outline_Vertex.shader"), Utils.LoadTextFile("src/Shaders/Outline_Fragment.shader"), "Axes");

        _sh_Skybox = new Shader(Utils.LoadTextFile("src/Shaders/Skybox_Vertex.shader"), Utils.LoadTextFile("src/Shaders/Skybox_Fragment.shader"), "Skybox");
        //_hdrTexture = new HdrTexture("src/hdr/autumn_field_puresky_4k.hdr");
        _hdrTexture_Skybox = new HdrTexture("src/hdr/rogland_clear_night_4k.hdr");
        ///_hdrTexture = new HdrTexture("src/hdr/grasslands_sunset_4k.hdr");
        ///_hdrTexture = new HdrTexture("src/hdr/overcast_soil_puresky_4k.hdr");
        //_hdrTexture = new HdrTexture("src/hdr/qwantani_dusk_2_puresky_4k.hdr");
        _skybox = new Skybox(_sh_Skybox, _hdrTexture_Skybox);
        _skybox.BlurScale = 3f;
        maxLod = MathF.Log2(MathF.Max(_hdrTexture_Skybox.Width, _hdrTexture_Skybox.Height));

        _mat_Lit = new Material { Color = Constants.lightGray, };
        _mat_Smooth = new Material { Color = Constants.lightGray, Roughness = 0f, };
        _mat_Matt = new Material { Color = Constants.lightGray, Roughness = 1f, };
        _mat_Metallic = new Material { Color = Constants.gray, Roughness = 0.05f, Metallic = 1, };
        _mat_MaterialPreview = new Material { Color = Constants.white, Roughness = 0, Metallic = 1 };

        _mat_Unlit = new Material { Color = Constants.gray, Roughness = 1f, };

        _mat_Axes = new Material { Color = Constants.gray, };
        _mat_GizmosG = new Material { Color = Constants.green, Alpha = 0.5f, };

        _mat_LitWhite = new Material { Color = Constants.white, };
        _mat_LitBlack = new Material { Color = Constants.black, };
        _mat_LitGray = new Material { Color = Constants.gray, };
        _mat_LitRed = new Material { Color = Constants.red, };
        _mat_LitGreen = new Material { Color = Constants.green, };
        _mat_LitBlue = new Material { Color = Constants.blue, };

        _mesh_Torus = Assets.Load<Mesh>(Path.Combine(Dirs.Models, "Torus.obj"));
        _mesh_Suzanne = Assets.Load<Mesh>(Path.Combine(Dirs.Models, "Suzanne.obj"));
        _mesh_SuzanneHighRes = Assets.Load<Mesh>(Path.Combine(Dirs.Models, "SuzanneHighRes.obj"));

        _gizmo_Selected = new GizmoSelected();

    }


    public readonly static Shader _sh_Lit = null!;
    public readonly static Shader _sh_Unlit = null!;
    public readonly static Shader _sh_Grid = null!;
    public readonly static Shader _sh_Axes = null!;
    public readonly static Shader _sh_Outline = null!;

    public readonly static Shader _sh_Skybox = null!;
    public readonly static Skybox _skybox = null!;
    public readonly static HdrTexture? _hdrTexture_Skybox = null;
    public readonly static float maxLod;
    public readonly static Material _mat_Axes = null!;
    public readonly static Material _mat_GizmosG = null!;

    public readonly static Material _mat_Lit = null!;
    public readonly static Material _mat_Unlit = null!;
    public readonly static Material _mat_Smooth = null!;
    public readonly static Material _mat_Matt = null!;
    public readonly static Material _mat_Metallic = null!;
    public readonly static Material _mat_MaterialPreview = null!;

    public readonly static Material _mat_LitWhite = null!;
    public readonly static Material _mat_LitBlack = null!;
    public readonly static Material _mat_LitGray = null!;
    public readonly static Material _mat_LitRed = null!;
    public readonly static Material _mat_LitGreen = null!;
    public readonly static Material _mat_LitBlue = null!;

    public readonly static Mesh _mesh_Cube = null!;
    public readonly static Mesh _mesh_CubeWireframe = null!;
    public readonly static Mesh _mesh_Sphere = null!;
    public readonly static Mesh _mesh_SphereWireframe = null!;
    public readonly static Mesh _mesh_Capsule = null!;
    public readonly static Mesh _mesh_CapsuleWireframe = null!;

    public readonly static Mesh _mesh_Plane = null!;
    public readonly static Mesh _mesh_PlaneWireframe = null!;
    public readonly static Mesh _mesh_PlaneQuad = null!;
    public readonly static Mesh _mesh_GridWireframe = null!;

    public readonly static Mesh _mesh_Torus = null!;
    public readonly static Mesh _mesh_Suzanne = null!;
    public readonly static Mesh _mesh_SuzanneHighRes = null!;

    public readonly static Mesh _mesh_AxesWireframe = null!;
    public readonly static Mesh _mesh_Arrow3D = null!;
    public readonly static Mesh _mesh_ArrowWireframe = null!;

    public readonly static GizmoSelected _gizmo_Selected = null!;


    internal static void OnClosing () {
        _mesh_Cube.Dispose();
        _mesh_Sphere.Dispose();
        _mesh_Arrow3D.Dispose();

        _sh_Lit.Dispose();
        _sh_Unlit.Dispose();
        _sh_Grid.Dispose();
        _sh_Axes.Dispose();

        _sh_Skybox.Dispose();
        _skybox.Dispose();
        _hdrTexture_Skybox?.Dispose();
    }

}
