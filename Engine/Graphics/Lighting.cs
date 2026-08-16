using static Engine.Graphics.Shader;

namespace Engine.Graphics;


public static class Lighting {

    private static List<LightSource> LightSources = new List<LightSource>();
    private static List<SunLight> SunLights = new List<SunLight>();

    public static SunLight? MainLight => 0 < SunLights.Count ? SunLights[0] : null;


    public static void SetSceneUniformsLit (Shader shader) {
        SunLight? light = MainLight;
        if (light is not null) {
            Vector3 dir = Mathf.QuaternionToDirection(light.Rotation);
            shader.SetVector3(SunLightDir, dir);
            shader.SetVector3(SunLightColor, light.Color);
            shader.SetFloat(SunLightIntensity, light.Intensity);
        }

        shader.SetVector3(AmbientColor, Constants.ambientColor);
        shader.SetFloat(AmbientColorIntensity, Constants.ambientColorIntensity);

        if (Constants.renderSkyboxReflection)
            shader.SetFloat(ReflectionIntensity, Constants.reflectionIntensity);
    }


    public static void RegisterLightSource (LightSource lightSource) {
        LightSources.Add(lightSource);
        switch (lightSource) {
            case SunLight sun:
                SunLights.Add(sun);
                break;
        }
    }
    public static void UnregisterLightSource (LightSource lightSource) {
        LightSources.Remove(lightSource);
        switch (lightSource) {
            case SunLight sun:
                SunLights.Remove(sun);
                break;
        }
    }

}
