using System.Linq;
using static Engine.Graphics.Shader;

namespace Engine.Graphics;


public static class Lighting {

    public static List<LightSource> LightSources = new List<LightSource>();
    public static List<SunLight> SunLights = new List<SunLight>();
    public static List<PointLight> PointLights = new List<PointLight>();

    public static SunLight? MainLight => 0 < SunLights.Count ? SunLights[0] : null;
    public const int MAX_SUN_LIGHTS = 32;
    public const int MAX_POINT_LIGHTS = 32;

    public const string PointLightPos = "uPointLightPos";
    public const string PointLightColor = "uPointLightColor";
    public const string PointLightIntensity = "uPointLightIntensity";
    public const string PointLightRange = "uPointLightRange";
    public const string PointLightCount = "uPointLightCount";


    public static void SetMainSunLight (SunLight sun) {
        //if (!LightSources.Contains(sun)) return;
        LightSources.Remove(sun);
        LightSources.Insert(0, sun);
    }


    public static void SetSceneUniformsLit (Shader shader) {
        List<SunLight> enabledLights = SunLights.Where(l => l.Enabled).ToList();
        int count = Math.Min(enabledLights.Count, MAX_SUN_LIGHTS);

        if (0 < count) {
            Vector3[] dirs = new Vector3[count];
            Vector3[] colors = new Vector3[count];
            float[] intensities = new float[count];

            for (int i = 0; i < count; i++) {
                //Log.log("SetSceneUniformsLit", i);
                SunLight light = enabledLights[i];
                dirs[i] = Mathf.QuaternionToDirection(light.Rotation);
                colors[i] = light.Color;
                intensities[i] = light.Intensity;
            }

            shader.SetVector3Array(SunLightDir, dirs);
            shader.SetVector3Array(SunLightColor, colors);
            shader.SetFloatArray(SunLightIntensity, intensities);
        }
        shader.SetInt(SunLightCount, count);

        List<PointLight> enabledPointLights = PointLights.Where(l => l.Enabled).ToList();
        int pointCount = Math.Min(enabledPointLights.Count, MAX_POINT_LIGHTS);

        if (0 < pointCount) {
            Vector3[] positions = new Vector3[pointCount];
            Vector3[] colors = new Vector3[pointCount];
            float[] intensities = new float[pointCount];
            float[] ranges = new float[pointCount];

            for (int i = 0; i < pointCount; i++) {
                PointLight light = enabledPointLights[i];
                positions[i] = light.Position;
                colors[i] = light.Color;
                intensities[i] = light.Intensity;
                ranges[i] = light.Range;
            }

            shader.SetVector3Array(PointLightPos, positions);
            shader.SetVector3Array(PointLightColor, colors);
            shader.SetFloatArray(PointLightIntensity, intensities);
            shader.SetFloatArray(PointLightRange, ranges);
        }
        shader.SetInt(PointLightCount, pointCount);

        /// General
        shader.SetVector3(AmbientColor, Constants.Ambient_Color);
        shader.SetFloat(AmbientColorIntensity, Constants.Ambient_Intensity);
        shader.SetFloat(Exposure, Camera.Main!.Exposure);

        if (Constants.renderSkyboxReflection)
            shader.SetFloat(ReflectionIntensity, Constants.reflectionIntensity);
    }

    public static void SetSHAmbient (Shader shader, in SHAmbientProbe probe) {
        shader.SetVector4("uSHAr", probe.SHAr);
        shader.SetVector4("uSHAg", probe.SHAg);
        shader.SetVector4("uSHAb", probe.SHAb);
        shader.SetVector4("uSHBr", probe.SHBr);
        shader.SetVector4("uSHBg", probe.SHBg);
        shader.SetVector4("uSHBb", probe.SHBb);
        shader.SetVector4("uSHC", probe.SHC);
        shader.SetFloat("uAmbientIntensity", probe.Intensity);
    }

    public static void RegisterLightSource (LightSource lightSource) {
        LightSources.Add(lightSource);
        switch (lightSource) {
            case SunLight sun:
                SunLights.Add(sun);
                break;
            case PointLight point:
                PointLights.Add(point);
                break;
        }
    }
    public static void UnregisterLightSource (LightSource lightSource) {
        LightSources.Remove(lightSource);
        switch (lightSource) {
            case SunLight sun:
                SunLights.Remove(sun);
                break;
            case PointLight point:
                PointLights.Remove(point);
                break;
        }
    }

}
