using Engine.Graphics;
using static Engine.Graphics.Shader;

namespace Engine;


public class AssetsEngine : Singleton<AssetsEngine> {
    static AssetsEngine () {
        Windows.Window.Closing += OnClosing;

        //Shader.StatsReset();

        //_sh_Lit = new Shader(Assets.LoadText("src/Shaders/Lit_Vertex.shader"), Assets.LoadText("src/Shaders/Lit_Fragment.shader"), "Lit");
        _sh_Lit_Instanced = new Shader("src/Shaders/Lit_Vertex.shader", "src/Shaders/Lit_Fragment.shader", "Lit", isLit: true);
        _sh_Lit_Instanced.Save("src/Shaders/Lit.shader");
        ///
        _sh_Unlit = new Shader("src/Shaders/Unlit_Vertex.shader", "src/Shaders/Unlit_Fragment.shader", "Unlit", isLit: false);
        _sh_Unlit.Save("src/Shaders/Unlit.shader");
        ///
        _sh_Unlit_Instanced = new Shader("src/Shaders/Unlit_Instanced_Vertex.shader", 
            "src/Shaders/Unlit_Fragment.shader", "Unlit_Instanced", isLit: false);
        _sh_Unlit_Instanced.Save("src/Shaders/Unlit_Instanced.shader");
        ///
        _sh_UI = new Shader("src/Shaders/UI_Vertex.shader", "src/Shaders/UI_Fragment.shader", "UI", isLit: false);
        _sh_UI.Save("src/Shaders/UI.shader");


        _mat_Lit = new Material(_sh_Lit_Instanced) { Name = "Lit", };
        _mat_Lit.SetVector3(uColor, Constants.white);
        _mat_Lit.Save("src/Materials/Lit.mat");
        ///
        _mat_Unlit = new Material(_sh_Unlit) { Name = "Unlit", };
        _mat_Unlit.SetVector3(uColor, Constants.white);
        _mat_Unlit.Save("src/Materials/Unlit.mat");
        ///
        _mat_UI = new Graphics.UI.MaterialUI(_sh_UI) { Name = "UI", };
        _mat_UI.Save("src/Materials/UI.mat");

        _sh_Skybox = new Shader("src/Shaders/Skybox_Vertex.shader",
            "src/Shaders/Skybox_Fragment.shader", "Skybox", isLit: false);
        _sh_Skybox.Save("src/Shaders/Skybox.shader");
        _sh_IBLPrefilter = new Shader("src/Shaders/PostProcessing/Fullscreen_Vertex.shader",
            "src/Shaders/IBLPrefilter_Fragment.shader", "IBLPrefilter", isLit: false);
        _mat_Skybox = new MaterialSkybox(_sh_Skybox);
        _mat_Skybox.Save("src/Materials/Skybox.mat");
        _hdr_Skybox = Assets.Load<HdrTexture>("src/HDR/autumn_field_puresky_4k.hdr");
        //_hdr_Skybox = Assets.Load<HdrTexture>("src/HDR/qwantani_dusk_2_puresky_4k.hdr");
        //_hdr_Skybox = Assets.Load<HdrTexture>("src/HDR/rogland_clear_night_4k.hdr");
        //_hdr_Skybox = Assets.Load<HdrTexture>("src/HDR/grasslands_sunset_4k.hdr");
        //_hdr_Skybox = Assets.Load<HdrTexture>("src/HDR/overcast_soil_puresky_4k.hdr");
        //if (_hdr_Skybox is not null) {
            _hdr_Skybox.Exposure = 0.5f;
            _hdr_Skybox.Save("src/HDR/Skybox.hdr");
        //}

        _sh_Reflection = new Shader("src/Shaders/Lit_Vertex.shader", 
            "src/Shaders/Reflection_Fragment.shader", "Reflection", isLit: false);
        _sh_Reflection.Save("src/Shaders/Reflection.shader");
        _mat_Reflection = new Material(_sh_Reflection);
        _mat_Reflection.Save("src/Materials/Reflection.mat");
        ///
        _sh_ShadowDepth = new Shader("src/Shaders/ShadowDepth_Vertex.shader", 
            "src/Shaders/ShadowDepth_Fragment.shader", "ShadowDepth", isLit: false);
        _sh_ShadowDepth.Save("src/Shaders/ShadowDepth.shader");
        _mat_ShadowDepth = new Material(_sh_ShadowDepth);
        _mat_ShadowDepth.Save("src/Materials/ShadowDepth.mat");
        ///
        _sh_PointShadowDepth = new Shader("src/Shaders/PointShadowDepth_Vertex.shader", 
            "src/Shaders/PointShadowDepth_Fragment.shader", "PointShadowDepth", isLit: false);
        _sh_PointShadowDepth.Save("src/Shaders/PointShadowDepth.shader");
        _mat_PointShadowDepth = new Material(_sh_PointShadowDepth);
        _mat_PointShadowDepth.Save("src/Materials/PointShadowDepth.mat");

        _mesh_Cube = Assets.Load<Mesh>(Path.Combine(Dirs.Models, "Cube.obj"))!;
        _mesh_Sphere = Assets.Load<Mesh>(Path.Combine(Dirs.Models, "Sphere.obj"))!;
        _mesh_Capsule = Assets.Load<Mesh>(Path.Combine(Dirs.Models, "Capsule.obj"))!;
        _mesh_Plane = Assets.Load<Mesh>(Path.Combine(Dirs.Models, "Plane.obj"))!;
        _mesh_PlaneQuad = Assets.Load<Mesh>(Path.Combine(Dirs.Models, "PlaneQuad.obj"))!;

        _sh_Text = new Shader("src/Shaders/UI/Text_Vertex.shader", "src/Shaders/UI/Text_Fragment.shader", "Text");
        _sh_Text.Save("src/Shaders/UI/Text.shader");
        _mat_Text = new Material(_sh_Text);
        _mat_Text.SetInt(Shader.uTexture, 0);
        _mat_Text.SetVector3(uColor, Constants.textRendererColor);
        _mat_Text.Save("src/Materials/UI/Text.mat");
        _font_Futura = File.ReadAllBytes("src/Fonts/FuturaCyrillicMedium.ttf");

        /// Editor
        _mat_Smooth = new Material(_mat_Lit) { Name = "Smooth", };
        _mat_Smooth.SetFloat(uSmoothness, 1);
        _mat_Smooth.Save("src/Materials/Smooth.mat");
        ///
        _mat_Matt = new Material(_mat_Lit) { Name = "Matt", };
        _mat_Matt.SetFloat(uSmoothness, 0);
        _mat_Matt.Save("src/Materials/Matt.mat");
        ///
        _mat_Metallic = new Material(_mat_Lit) { Name = "Metallic", };
        _mat_Metallic.SetVector3(uColor, Constants.gray);
        _mat_Metallic.SetFloat(uMetallic, 1);
        _mat_Metallic.Save("src/Materials/Metallic.mat");
        ///
        _mat_MaterialPreview = new Material(_mat_Lit) { Name = "Material_Preview", };
        _mat_MaterialPreview.SetVector3(uColor, Constants.white);
        _mat_MaterialPreview.SetFloat(uSmoothness, 1);
        _mat_MaterialPreview.SetFloat(uMetallic, 1);
        _mat_MaterialPreview.Save("src/Materials/Material_Preview.mat");
        ///
        _mat_LitWhite = new Material(_sh_Lit_Instanced) { Name = "Lit_White", };
        _mat_LitWhite.SetVector3(uColor, Constants.white);
        _mat_LitWhite.Save("src/Materials/Lit_White.mat");
        ///
        _mat_LitBlack = new Material(_sh_Lit_Instanced) { Name = "Lit_Black", };
        _mat_LitBlack.SetVector3(uColor, Constants.black);
        _mat_LitBlack.Save("src/Materials/Lit_Black.mat");
        ///
        _mat_LitGray = new Material(_sh_Lit_Instanced) { Name = "Lit_Gray", };
        _mat_LitGray.SetVector3(uColor, Constants.gray);
        _mat_LitGray.Save("src/Materials/Lit_Gray.mat");
        ///
        _mat_LitRed = new Material(_sh_Lit_Instanced) { Name = "Lit_Red", };
        _mat_LitRed.SetVector3(uColor, Constants.red);
        _mat_LitRed.Save("src/Materials/Lit_Red.mat");
        ///
        _mat_LitGreen = new Material(_sh_Lit_Instanced) { Name = "Lit_Green", };
        _mat_LitGreen.SetVector3(uColor, Constants.green);
        _mat_LitGreen.Save("src/Materials/Lit_Green.mat");
        ///
        _mat_LitBlue = new Material(_sh_Lit_Instanced) { Name = "Lit_Blue", };
        _mat_LitBlue.SetVector3(uColor, Constants.blue);
        _mat_LitBlue.Save("src/Materials/Lit_Blue.mat");

        _mesh_Torus = Assets.Load<Mesh>(Path.Combine(Dirs.Models, "Torus.obj"))!;
        _mesh_Suzanne = Assets.Load<Mesh>(Path.Combine(Dirs.Models, "Suzanne.obj"))!;
        _mesh_SuzanneHighRes = Assets.Load<Mesh>(Path.Combine(Dirs.Models, "SuzanneHighRes.obj"))!;

        /// Post-Process Effects
        _sh_Tonemap = new Shader("src/Shaders/PostProcessing/Fullscreen_Vertex.shader", 
            "src/Shaders/PostProcessing/Tonemap_Fragment.shader", "Tonemap");
        _sh_Tonemap.Save("src/Shaders/Tonemap.shader");
        _mat_Tonemap = new Material(_sh_Tonemap);
        _mat_Tonemap.Save("src/Materials/Tonemap.mat");

        _sh_Depth = new Shader("src/Shaders/PostProcessing/Fullscreen_Vertex.shader", 
            "src/Shaders/PostProcessing/Depth_Fragment.shader", "Depth");
        _sh_Depth.Save("src/Shaders/Depth.shader");
        _mat_Depth = new Material(_sh_Depth);
        _mat_Depth.Save("src/Materials/Depth.mat");

        _sh_Grayscale = new Shader("src/Shaders/PostProcessing/Fullscreen_Vertex.shader", 
            "src/Shaders/PostProcessing/Grayscale_Fragment.shader", "Grayscale");
        _sh_Grayscale.Save("src/Shaders/Grayscale.shader");
        _mat_Grayscale = new Material(_sh_Grayscale);
        _mat_Grayscale.Save("src/Materials/Grayscale.mat");

        _sh_Fxaa = new Shader("src/Shaders/PostProcessing/Fullscreen_Vertex.shader", 
            "src/Shaders/PostProcessing/Fxaa_Fragment.shader", "FXAA");
        _sh_Fxaa.Save("src/Shaders/Fxaa.shader");
        _mat_Fxaa = new MaterialFxaa(_sh_Fxaa);
        _mat_Fxaa.Save("src/Materials/Fxaa.mat");

        _sh_SSAO = new Shader("src/Shaders/PostProcessing/Fullscreen_Vertex.shader", 
            "src/Shaders/PostProcessing/SSAO_Fragment.shader", "SSAO");
        _sh_SSAO.Save("src/Shaders/SSAO.shader");
        _mat_SSAO = new MaterialSSAO(_sh_SSAO);
        _mat_SSAO.Save("src/Materials/SSAO.mat");

        _sh_SSAO_Blur = new Shader("src/Shaders/PostProcessing/Fullscreen_Vertex.shader", 
            "src/Shaders/PostProcessing/SSAO_Blur_Fragment.shader", "SSAO_Blur");
        _sh_SSAO_Blur.Save("src/Shaders/SSAO_Blur.shader");
        _mat_SSAO_Blur = new MaterialSSAOBlur(_sh_SSAO_Blur);
        _mat_SSAO_Blur.Save("src/Materials/SSAO_Blur.mat");

        _sh_SSAO_Composite = new Shader("src/Shaders/PostProcessing/Fullscreen_Vertex.shader", 
            "src/Shaders/PostProcessing/SSAO_Composite_Fragment.shader", "SSAO_Composite");
        _sh_SSAO_Composite.Save("src/Shaders/SSAO_Composite.shader");
        _mat_SSAO_Composite = new MaterialSSAOComposite(_sh_SSAO_Composite);
        _mat_SSAO_Composite.Save("src/Materials/SSAO_Composite.mat");

        _sh_Vignette = new Shader("src/Shaders/PostProcessing/Fullscreen_Vertex.shader", 
            "src/Shaders/PostProcessing/Vignette_Fragment.shader", "Vignette");
        _sh_Vignette.Save("src/Shaders/Vignette.shader");
        _mat_Vignette = new Material(_sh_Vignette);
        _mat_Vignette.SetFloat("uVignetteIntensity", 0.5f);
        _mat_Vignette.SetFloat("uVignetteRadius", 0.35f);
        _mat_Vignette.SetFloat("uVignetteSoftness", 1f);
        _mat_Vignette.SetVector3("uVignetteColor", Vector3.Zero);
        _mat_Vignette.Save("src/Materials/Vignette.mat");

        _sh_CameraFocus = new Shader("src/Shaders/PostProcessing/Fullscreen_Vertex.shader", 
            "src/Shaders/PostProcessing/CameraFocus_Fragment.shader", "CameraFocus");
        _sh_CameraFocus.Save("src/Shaders/CameraFocus.shader");
        _mat_CameraFocus = new MaterialCameraFocus(_sh_CameraFocus);
        _mat_CameraFocus.SetInt(Shader.uDepth, 1);
        //_mat_CameraFocus.SetFloat("uNear", Renderer.Instance.Camera.PlaneNear);
        //_mat_CameraFocus.SetFloat("uFar", Renderer.Instance.Camera.PlaneFar);
        _mat_CameraFocus.SetFloat("uFocusDistance", 10f);
        _mat_CameraFocus.SetFloat("uFocusRange", 5f);
        _mat_CameraFocus.SetFloat("uBokehRadius", 3f);
        //_mat_CameraFocus.SetVector2("uTexelSize", new Vector2(1f/Renderer.Instance.Width, 1f/Renderer.Instance.Height));
        _mat_CameraFocus.Save("src/Materials/CameraFocus.mat");

        tex_Test = Texture.Load("src/Images/RGBA_Test.png");
        tex_White = Texture.Load("src/Images/white.png");
        tex_Vignette = Texture.Load("src/Images/vignette.png");

    }


    /// Main
    public readonly static Shader _sh_Lit_Instanced = null!;
    public readonly static Shader _sh_Unlit = null!;
    public readonly static Shader _sh_Unlit_Instanced = null!;
    public readonly static Shader _sh_UI = null!;

    public readonly static Material _mat_Lit = null!;
    public readonly static Material _mat_Unlit = null!;
    public readonly static Graphics.UI.MaterialUI _mat_UI = null!;

    public readonly static Shader _sh_Skybox = null!;
    public readonly static Shader _sh_IBLPrefilter = null!;
    public readonly static Material _mat_Skybox = null!;
    public readonly static HdrTexture? _hdr_Skybox = null;

    public readonly static Shader _sh_Reflection = null!;
    public readonly static Material _mat_Reflection = null!;

    public readonly static Shader _sh_ShadowDepth = null!;
    public readonly static Material _mat_ShadowDepth = null!;

    public readonly static Shader _sh_PointShadowDepth = null!;
    public readonly static Material _mat_PointShadowDepth = null!;
    
    public readonly static Mesh _mesh_Cube = null!;
    public readonly static Mesh _mesh_Sphere = null!;
    public readonly static Mesh _mesh_Capsule = null!;
    public readonly static Mesh _mesh_Plane = null!;
    public readonly static Mesh _mesh_PlaneQuad = null!;

    public readonly static Shader _sh_Text = null!;
    public readonly static Material _mat_Text = null!;
    public readonly static byte[] _font_Futura = null!;

    /// Post-Process Effects
    public readonly static Shader _sh_Tonemap = null!;
    public readonly static Material _mat_Tonemap = null!;
    public readonly static Shader _sh_Depth = null!;
    public readonly static Material _mat_Depth = null!;
    public readonly static Shader _sh_Grayscale = null!;
    public readonly static Material _mat_Grayscale = null!;
    public readonly static Shader _sh_Fxaa = null!;
    public readonly static Material _mat_Fxaa = null!;
    public readonly static Shader _sh_SSAO = null!;
    public readonly static Material _mat_SSAO = null!;
    public readonly static Shader _sh_SSAO_Blur = null!;
    public readonly static Material _mat_SSAO_Blur = null!;
    public readonly static Shader _sh_SSAO_Composite = null!;
    public readonly static Material _mat_SSAO_Composite = null!;
    public readonly static Shader _sh_Vignette = null!;
    public readonly static Material _mat_Vignette = null!;
    public readonly static Shader _sh_CameraFocus = null!;
    public readonly static Material _mat_CameraFocus = null!;


    public readonly static Texture tex_Test = null!;
    public readonly static Texture tex_White = null!;
    public readonly static Texture tex_Vignette = null!;


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

        _sh_Lit_Instanced.Dispose();
        _sh_Unlit.Dispose();
        _sh_Depth.Dispose();
        _sh_Grayscale.Dispose();
        _sh_Fxaa.Dispose();

        _sh_Depth.Dispose();
        _sh_Grayscale.Dispose();
        _sh_Fxaa.Dispose();
        _sh_SSAO.Dispose();
        _sh_SSAO_Blur.Dispose();
        _sh_SSAO_Composite.Dispose();
        _sh_CameraFocus.Dispose();

        _sh_Skybox.Dispose();
        _hdr_Skybox?.Dispose();
    }

}
