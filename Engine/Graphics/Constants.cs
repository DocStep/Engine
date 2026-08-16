namespace Engine.Graphics;

public enum DrawMode {
    Normal = 0,
    Wireframe,
    NormalWireframe,
}


public static class Constants {

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

    //public readonly static Vector3 clearColor = new Vector3(0.1f, 0.1f, 0.15f);
    public readonly static Vector3 clearColor = black;
    public readonly static float _gridScale = 500;
    public readonly static float _gridDivisionScale = 0.5f;

    public static bool _drawArrowAsMesh = true;

    public static Vector3 Ambient_Color = new Vector3(1f, 0f, 0f);
    public static float Ambient_Intensity = 0.1f;

    public static Vector3 Light_Color = new Vector3(1f, 1f, 1f);
    public static float Light_Intensity = 3f;

    public static Vector3 SunLight_Euler = new Vector3(60f, -30f, 0);
    public static float PointLight_Radius = 5f;

    public static bool renderSkybox { get; private set; } = true;
    public static bool renderSkyboxReflection { get; private set; } = true;
    public static float reflectionIntensity = 1f;

    public static bool drawMaterialsGrid = false;
    public static int materialsGridCount = 5;
    public static int materialsGridDensity = 1;

    public static bool drawGizmos = true;

    public static DrawMode drawMode = DrawMode.Normal;


    public static int textRendererMarginLeft = 10;
    public static Vector3 textRendererColor = 0.2f*Vector3.One;


}
