using System.Numerics;

namespace Engine.Graphics;


internal static class Constants {

    public readonly static Vector3 white = new Vector3(1, 1, 1);
    public readonly static Vector3 black = new Vector3(0, 0, 0);

    public readonly static Vector3 lightGray = new Vector3(0.8f, 0.8f, 0.8f);
    public readonly static Vector3 gray = new Vector3(0.5f, 0.5f, 0.5f);
    public readonly static Vector3 darkGray = new Vector3(0.25f, 0.25f, 0.25f);

    public readonly static Vector3 red = new Vector3(1f, 0f, 0f);
    public readonly static Vector3 green = new Vector3(0f, 1f, 0f);
    public readonly static Vector3 blue = new Vector3(0f, 0f, 1f);

    public readonly static Vector3 magenta = new Vector3(1f, 0f, 1f);
    public readonly static Vector3 yellow = new Vector3(1f, 1f, 0f);
    public readonly static Vector3 cyan = new Vector3(0f, 1f, 1f);


    internal static float _cameraFOV = 0.25f*MathF.PI;
    internal static float _cameraPlaneClose = 0.1f;
    internal static float _cameraPlaneFar = 1000f;

    internal static bool _renderSkybox = true;

    internal readonly static float _gridScale = _cameraPlaneFar;
    internal readonly static float _gridDivisionScale = 1f;

    internal static bool _drawArrowAsMesh = true;

    internal static Vector3 sunLightDir = Vector3.Normalize(new Vector3(-0.3f, -1f, 0.4f));
    internal static Vector3 sunLightColor = new Vector3(1f, 1f, 1f);
    internal static float sunLightIntensity = 5f;
    internal static float reflectionIntensity = 1f;

    internal static bool drawTestGrid = false;
    internal static int testGridCount = 5;
    internal static int testGridDensity = 1;

    internal static bool drawGizmos = true;
    internal static bool drawGizmosSun = true;


    internal static int left = 10;

}
