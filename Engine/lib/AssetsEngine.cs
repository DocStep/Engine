using Engine.Graphics;
using static Engine.Graphics.Shader;

namespace Engine;


public class AssetsEngine : Singleton<AssetsEngine> {
    static AssetsEngine () {
        Engine.Window.Closing += OnClosing;

        _mesh_Cube = new Mesh(Cube.Generate());
        _mesh_Sphere = new Mesh(Sphere.Generate());
        _mesh_Capsule = new Mesh(Capsule.Generate());
        _mesh_Plane = new Mesh(Graphics.Plane.Generate());
        _mesh_PlaneQuad = new Mesh(Graphics.Plane.Generate(divisions: 1));

        _sh_Lit = new Shader(Utils.LoadTextFile("src/Shaders/Lit_Vertex.shader"), Utils.LoadTextFile("src/Shaders/Lit_Fragment.shader"), "Lit");
        //_sh_Transparent = new Shader(Utils.LoadTextFile("src/Shaders/Lit_Vertex.shader"), Utils.LoadTextFile("src/Shaders/Lit_Fragment.shader"), "Transparent");
        _sh_Unlit = new Shader(Utils.LoadTextFile("src/Shaders/Unlit_Vertex.shader"), Utils.LoadTextFile("src/Shaders/Unlit_Fragment.shader"), "Unlit");
        //_sh_Unlit!.pass = RenderPass.Gizmo;
        //_sh_UnlitTransparent = new Shader(Utils.LoadTextFile("src/Shaders/Unlit_Vertex.shader"), Utils.LoadTextFile("src/Shaders/Unlit_Fragment.shader"), "UnlitTransparent");
        //_sh_UnlitTransparent!.pass = RenderPass.Gizmo;
        
        _sh_Skybox = new Shader(Utils.LoadTextFile("src/Shaders/Skybox_Vertex.shader"), Utils.LoadTextFile("src/Shaders/Skybox_Fragment.shader"), "Skybox");
        //_hdrTexture = new HdrTexture("src/hdr/autumn_field_puresky_4k.hdr");
        _hdrTexture_Skybox = new HdrTexture("src/hdr/rogland_clear_night_4k.hdr");
        ///_hdrTexture = new HdrTexture("src/hdr/grasslands_sunset_4k.hdr");
        ///_hdrTexture = new HdrTexture("src/hdr/overcast_soil_puresky_4k.hdr");
        //_hdrTexture = new HdrTexture("src/hdr/qwantani_dusk_2_puresky_4k.hdr");
        _skybox = new Skybox(_sh_Skybox, _hdrTexture_Skybox);
        _skybox.BlurScale = 3f;
        maxLod = MathF.Log2(MathF.Max(_hdrTexture_Skybox.Width, _hdrTexture_Skybox.Height));

        _mat_Lit = new Material(_sh_Lit);
        _mat_Lit.SetVector3(Color, Constants.red);

        _mat_Smooth = new Material(_sh_Unlit);
        _mat_Smooth.SetVector3(Color, Constants.lightGray);
        _mat_Smooth.SetFloat(Smoothness, 1);

        _mat_Matt = new Material(_sh_Unlit);
        _mat_Matt.SetVector3(Color, Constants.lightGray);
        _mat_Matt.SetFloat(Smoothness, 0);

        _mat_Metallic = new Material(_sh_Lit);
        _mat_Metallic.SetVector3(Color, Constants.gray);
        _mat_Metallic.SetFloat(Metallic, 1);

        _mat_MaterialPreview = new Material(_sh_Lit);
        _mat_MaterialPreview.SetVector3(Color, Constants.white);

        _mat_Unlit = new Material(_sh_Unlit);
        _mat_Unlit.SetVector3(Color, Constants.gray);
        _mat_Unlit.SetFloat(Smoothness, 1f);

        _mat_LitWhite = new Material(_sh_Lit);
        _mat_LitWhite.SetVector3(Color, Constants.white);

        _mat_LitBlack = new Material(_sh_Lit);
        _mat_LitBlack.SetVector3(Color, Constants.black);

        _mat_LitGray = new Material(_sh_Lit);
        _mat_LitGray.SetVector3(Color, Constants.gray);

        _mat_LitRed = new Material(_sh_Lit);
        _mat_LitRed.SetVector3(Color, Constants.red);

        _mat_LitGreen = new Material(_sh_Lit);
        _mat_LitGreen.SetVector3(Color, Constants.green);

        _mat_LitBlue = new Material(_sh_Lit);
        _mat_LitBlue.SetVector3(Color, Constants.blue);

        _mesh_Torus = Assets.Load<Mesh>(Path.Combine(Dirs.Models, "Torus.obj"));
        _mesh_Suzanne = Assets.Load<Mesh>(Path.Combine(Dirs.Models, "Suzanne.obj"));
        _mesh_SuzanneHighRes = Assets.Load<Mesh>(Path.Combine(Dirs.Models, "SuzanneHighRes.obj"));

    }


    public readonly static Shader _sh_Lit = null!;
    public readonly static Shader _sh_Transparent = null!;
    public readonly static Shader _sh_Unlit = null!;
    public readonly static Shader _sh_UnlitTransparent = null!;

    public readonly static Shader _sh_Skybox = null!;
    public readonly static Skybox _skybox = null!;
    public readonly static HdrTexture? _hdrTexture_Skybox = null;
    public readonly static float maxLod;

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
    public readonly static Mesh _mesh_Sphere = null!;
    public readonly static Mesh _mesh_Capsule = null!;
    public readonly static Mesh _mesh_Plane = null!;
    public readonly static Mesh _mesh_PlaneQuad = null!;

    public readonly static Mesh _mesh_Torus = null!;
    public readonly static Mesh _mesh_Suzanne = null!;
    public readonly static Mesh _mesh_SuzanneHighRes = null!;


    internal static void OnClosing () {
        _mesh_Cube.Dispose();
        _mesh_Sphere.Dispose();
        _mesh_Capsule.Dispose();
        _mesh_Plane.Dispose();
        _mesh_PlaneQuad.Dispose();

        _sh_Lit.Dispose();
        _sh_Unlit.Dispose();

        _sh_Skybox.Dispose();
        _skybox.Dispose();
        _hdrTexture_Skybox?.Dispose();
    }

}
