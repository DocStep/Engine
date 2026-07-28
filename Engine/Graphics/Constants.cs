namespace Engine.Graphics;

public enum DrawMode {
    Normal = 0,
    Wireframe,
    NormalWireframe,
}


internal static class Constants {

    public readonly static Vector3 white = new Vector3(1, 1, 1);
    public readonly static Vector3 black = new Vector3(0, 0, 0);

    public readonly static Vector3 lightGray = new Vector3(0.8f, 0.8f, 0.8f);
    public readonly static Vector3 gray = new Vector3(0.5f, 0.5f, 0.5f);
    public readonly static Vector3 darkGray = new Vector3(0.25f, 0.25f, 0.25f);

    public readonly static Vector3 red = new Vector3(1, 0, 0);
    public readonly static Vector3 green = new Vector3(0, 1, 0);
    public readonly static Vector3 blue = new Vector3(0, 0, 1);

    public readonly static Vector3 magenta = new Vector3(1, 0, 1);
    public readonly static Vector3 yellow = new Vector3(1, 1, 0);
    public readonly static Vector3 cyan = new Vector3(0, 1, 1);

    public readonly static Vector3 redLight = new Vector3(1, 0.5f, 0.5f);
    public readonly static Vector3 greenLight = new Vector3(0.5f, 1, 0.5f);
    public readonly static Vector3 blueLight = new Vector3(0.5f, 0.5f, 1);

    public static float _cameraFOV = 0.25f*MathF.PI;
    public static float _cameraPlaneClose = 0.1f;
    public static float _cameraPlaneFar = 1000f;

    public readonly static float _gridScale = _cameraPlaneFar;
    public readonly static float _gridDivisionScale = 1f;

    public static bool _drawArrowAsMesh = true;

    public static Vector3 sunLightDir = Vector3.Normalize(new Vector3(-0.3f, -1f, 0.4f));
    public static Vector3 sunLightColor = new Vector3(1f, 1f, 1f);
    public static Vector3 ambientColor = new Vector3(0.5f, 0.5f, 0.6f);
    public static float ambientColorIntensity = 0.1f;
    public static float sunLightIntensity = 5f;

    public static bool renderSkybox { get; private set; } = true;
    public static bool renderSkyboxReflection { get; private set; } = true;
    public static float reflectionIntensity = 1f;

    public static bool drawMaterialsGrid = true;
    public static int materialsGridCount = 5;
    public static int materialsGridDensity = 1;

    public static bool drawGizmos = true;
    public static bool drawGizmosSun = true;

    public static DrawMode drawMode = DrawMode.Normal;


    internal static int textRendererMarginLeft = 10;

}
