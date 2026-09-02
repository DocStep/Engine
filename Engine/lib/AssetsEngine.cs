using Engine.Graphics;
using static Engine.Graphics.Shader;

namespace Engine;


public class AssetsEngine : Singleton<AssetsEngine> {
    static AssetsEngine () {
        Windows.Window.Closing += OnClosing;

        //Shader.StatsReset();

        //_sh_Lit = new Shader(Assets.LoadText("src/Shaders/Lit_Vertex.shader"), Assets.LoadText("src/Shaders/Lit_Fragment.shader"), "Lit");
        _sh_Lit = new Shader(Assets.LoadText("src/Shaders/Lit_Vertex.shader"), Assets.LoadText("src/Shaders/LitAlt_Fragment.shader"), "Lit", isLit: true);
        _sh_Unlit = new Shader(Assets.LoadText("src/Shaders/Unlit_Vertex.shader"), Assets.LoadText("src/Shaders/Unlit_Fragment.shader"), "Unlit", isLit: false);
        _sh_UI = new Shader(Assets.LoadText("src/Shaders/UI_Vertex.shader"), Assets.LoadText("src/Shaders/UI_Fragment.shader"), "UI", isLit: false);
        
        _mat_Lit = new Material(_sh_Lit) { Name = "Lit", };
        _mat_Lit.SetVector3(Color, Constants.white);
        _mat_Lit.SetFloat(Smoothness, 0.5f);
        _mat_Lit.SetFloat(Metallic, 0);
        ///
        _mat_Unlit = new Material(_sh_Unlit) { Name = "Unlit", };
        _mat_Unlit.SetVector3(Color, Constants.gray);
        ///
        _mat_UI = new Graphics.UI.MaterialUI(_sh_UI) { Name = "UI", };

        _sh_Skybox = new Shader(Assets.LoadText("src/Shaders/Skybox_Vertex.shader"), Assets.LoadText("src/Shaders/Skybox_Fragment.shader"), "Skybox", isLit: false);
        _mat_Skybox = new MaterialSkybox(_sh_Skybox);
        _hdr_Skybox = new HdrTexture("src/hdr/autumn_field_puresky_4k.hdr");
        //_hdr_Skybox = new HdrTexture("src/hdr/rogland_clear_night_4k.hdr");
        //_hdr_Skybox = new HdrTexture("src/hdr/grasslands_sunset_4k.hdr");
        //_hdr_Skybox = new HdrTexture("src/hdr/overcast_soil_puresky_4k.hdr");
        //_hdr_Skybox = new HdrTexture("src/hdr/qwantani_dusk_2_puresky_4k.hdr");

        _mesh_Cube = new Mesh(Cube.Generate());
        _mesh_Sphere = new Mesh(Sphere.Generate());
        _mesh_Capsule = new Mesh(Capsule.Generate());
        _mesh_Plane = new Mesh(Graphics.Plane.Generate());
        _mesh_PlaneQuad = new Mesh(Graphics.Plane.Generate(divisions: 1));

        _sh_Text = new Shader(Assets.LoadText("src/Shaders/UI/Text_Vertex.shader"), Assets.LoadText("src/Shaders/UI/Text_Fragment.shader"), "Text");
        _mat_Text = new Material(_sh_Text) { Name = "Text", };
        _mat_Text.SetInt(Shader.Texture, 0);
        _mat_Text.SetVector3(Color, Constants.textRendererColor);
        _fontData = File.ReadAllBytes("src/Fonts/FuturaCyrillicMedium.ttf");

        /// Editor
        _mat_Smooth = new Material(_mat_Lit) { Name = "Smooth", };
        _mat_Smooth.SetFloat(Smoothness, 1);
        ///
        _mat_Matt = new Material(_mat_Lit) { Name = "Matt", };
        _mat_Matt.SetFloat(Smoothness, 0);
        ///
        _mat_Metallic = new Material(_mat_Lit) { Name = "Metallic", };
        _mat_Metallic.SetVector3(Color, Constants.gray);
        _mat_Metallic.SetFloat(Metallic, 1);
        ///
        _mat_MaterialPreview = new Material(_mat_Lit) { Name = "Material Preview", };
        _mat_MaterialPreview.SetVector3(Color, Constants.white);
        _mat_MaterialPreview.SetFloat(Smoothness, 1);
        _mat_MaterialPreview.SetFloat(Metallic, 1);
        ///
        _mat_LitWhite = new Material(_sh_Lit) { Name = "Lit White", };
        _mat_LitWhite.SetVector3(Color, Constants.white);
        ///
        _mat_LitBlack = new Material(_sh_Lit) { Name = "Lit Black", };
        _mat_LitBlack.SetVector3(Color, Constants.black);
        ///
        _mat_LitGray = new Material(_sh_Lit) { Name = "Lit Gray", };
        _mat_LitGray.SetVector3(Color, Constants.gray);
        ///
        _mat_LitRed = new Material(_sh_Lit) { Name = "Lit Red", };
        _mat_LitRed.SetVector3(Color, Constants.red);
        ///
        _mat_LitGreen = new Material(_sh_Lit) { Name = "Lit Green", };
        _mat_LitGreen.SetVector3(Color, Constants.green);
        ///
        _mat_LitBlue = new Material(_sh_Lit) { Name = "Lit Blue", };
        _mat_LitBlue.SetVector3(Color, Constants.blue);

        _mesh_Torus = Assets.Load<Mesh>(Path.Combine(Dirs.Models, "Torus.obj"));
        _mesh_Suzanne = Assets.Load<Mesh>(Path.Combine(Dirs.Models, "Suzanne.obj"));
        _mesh_SuzanneHighRes = Assets.Load<Mesh>(Path.Combine(Dirs.Models, "SuzanneHighRes.obj"));

        /// Post-Process Effects
        _sh_Depth = new Shader(Assets.LoadText("src/Shaders/PostProcessing/Fullscreen_Vertex.shader"), Assets.LoadText("src/Shaders/PostProcessing/Depth_Fragment.shader"), "Depth");
        _mat_Depth = new Material(_sh_Depth) { Name = "Depth", };

        _sh_Grayscale = new Shader(Assets.LoadText("src/Shaders/PostProcessing/Fullscreen_Vertex.shader"), Assets.LoadText("src/Shaders/PostProcessing/Grayscale_Fragment.shader"), "Grayscale");
        _mat_Grayscale = new Material(_sh_Grayscale) { Name = "Grayscale", };

        _sh_Fxaa = new Shader(Assets.LoadText("src/Shaders/PostProcessing/Fullscreen_Vertex.shader"), Assets.LoadText("src/Shaders/PostProcessing/Fxaa_Fragment.shader"), "FXAA");
        _mat_Fxaa = new MaterialFxaa(_sh_Fxaa) { Name = "FXAA", };

        _sh_SSAO = new Shader(Assets.LoadText("src/Shaders/PostProcessing/Fullscreen_Vertex.shader"), Assets.LoadText("src/Shaders/PostProcessing/SSAO_Fragment.shader"), "SSAO");
        _mat_SSAO = new MaterialSSAO(_sh_SSAO) { Name = "SSAO", };

        _sh_SSAOBlur = new Shader(Assets.LoadText("src/Shaders/PostProcessing/Fullscreen_Vertex.shader"), Assets.LoadText("src/Shaders/PostProcessing/SSAOBlur_Fragment.shader"), "SSAOBlur");
        _mat_SSAOBlur = new MaterialSSAOBlur(_sh_SSAOBlur) { Name = "SSAO Blur", };

        _sh_SSAOComposite = new Shader(Assets.LoadText("src/Shaders/PostProcessing/Fullscreen_Vertex.shader"), Assets.LoadText("src/Shaders/PostProcessing/SSAOComposite_Fragment.shader"), "SSAOComposite");
        _mat_SSAOComposite = new MaterialSSAOComposite(_sh_SSAOComposite) { Name = "SSAO Composite", };

        _sh_Vignette = new Shader(Assets.LoadText("src/Shaders/PostProcessing/Fullscreen_Vertex.shader"), Assets.LoadText("src/Shaders/PostProcessing/Vignette_Fragment.shader"), "CameraFocus");
        _mat_Vignette = new MaterialVignette(_sh_Vignette) { Name = "Vignette", };

        _sh_CameraFocus = new Shader(Assets.LoadText("src/Shaders/PostProcessing/Fullscreen_Vertex.shader"), Assets.LoadText("src/Shaders/PostProcessing/CameraFocus_Fragment.shader"), "CameraFocus");
        _mat_CameraFocus = new MaterialCameraFocus(_sh_CameraFocus) { Name = "CameraFocus", };

        Texture tex_Test = Texture.Load("src/Images/RGBA_Test.png");
        Texture tex_White = Texture.Load("src/Images/white.png");
        Texture tex_Vignette = Texture.Load("src/Images/vignette.png");
    }


    /// Editor
    public readonly static Shader _sh_Lit = null!;
    public readonly static Shader _sh_Unlit = null!;
    public readonly static Shader _sh_UI = null!;

    public readonly static Material _mat_Lit = null!;
    public readonly static Material _mat_Unlit = null!;
    public readonly static Graphics.UI.MaterialUI _mat_UI = null!;

    public readonly static Shader _sh_Skybox = null!;
    public readonly static Material _mat_Skybox = null!;
    public readonly static HdrTexture? _hdr_Skybox = null;
    
    public readonly static Mesh _mesh_Cube = null!;
    public readonly static Mesh _mesh_Sphere = null!;
    public readonly static Mesh _mesh_Capsule = null!;
    public readonly static Mesh _mesh_Plane = null!;
    public readonly static Mesh _mesh_PlaneQuad = null!;

    public readonly static Shader _sh_Text = null!;
    public readonly static Material _mat_Text = null!;
    public readonly static byte[] _fontData = null!;

    /// Post-Process Effects
    public readonly static Shader _sh_Depth = null!;
    public readonly static Material _mat_Depth = null!;
    public readonly static Shader _sh_Grayscale = null!;
    public readonly static Material _mat_Grayscale = null!;
    public readonly static Shader _sh_Fxaa = null!;
    public readonly static Material _mat_Fxaa = null!;
    public readonly static Shader _sh_SSAO = null!;
    public readonly static Material _mat_SSAO = null!;
    public readonly static Shader _sh_SSAOBlur = null!;
    public readonly static Material _mat_SSAOBlur = null!;
    public readonly static Shader _sh_SSAOComposite = null!;
    public readonly static Material _mat_SSAOComposite = null!;
    public readonly static Shader _sh_Vignette = null!;
    public readonly static Material _mat_Vignette = null!;
    public readonly static Shader _sh_CameraFocus = null!;
    public readonly static Material _mat_CameraFocus = null!;


    public readonly static Texture tex_Test = Texture.Load("src/Images/RGBA_Test.png");
    public readonly static Texture tex_White = Texture.Load("src/Images/white.png");
    public readonly static Texture tex_Vignette = Texture.Load("src/Images/vignette.png");


    public readonly static Mesh _mesh_Torus = null!;
    public readonly static Mesh _mesh_Suzanne = null!;
    public readonly static Mesh _mesh_SuzanneHighRes = null!;


    /// Template
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



    internal static void OnClosing () {
        _mesh_Cube.Dispose();
        _mesh_Sphere.Dispose();
        _mesh_Capsule.Dispose();
        _mesh_Plane.Dispose();
        _mesh_PlaneQuad.Dispose();

        _sh_Lit.Dispose();
        _sh_Unlit.Dispose();
        _sh_Depth.Dispose();
        _sh_Grayscale.Dispose();
        _sh_Fxaa.Dispose();

        _sh_Depth.Dispose();
        _sh_Grayscale.Dispose();
        _sh_Fxaa.Dispose();
        _sh_SSAO.Dispose();
        _sh_SSAOBlur.Dispose();
        _sh_SSAOComposite.Dispose();
        _sh_CameraFocus.Dispose();

        _sh_Skybox.Dispose();
        _hdr_Skybox?.Dispose();
    }

}
