using System.Linq;
using Silk.NET.OpenGL;
using static Engine.Graphics.Shader;

namespace Engine.Graphics;


public static class Lighting {

    [Hide] public static List<LightSource> LightSources = new List<LightSource>();
    [Hide] public static List<SunLight> SunLights = new List<SunLight>();
    [Hide] public static List<PointLight> PointLights = new List<PointLight>();

    [Hide] public static SunLight? MainLight => 0 < SunLights.Count ? SunLights[0] : null;

    [Range(0, int.MaxValue)] public static int SunLights_Max = 32;
    [Range(0, int.MaxValue)] public static int PoinyLights_Max = 32;



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

    /// The sun that casts the shadow. Must be enabled, or the shadow map and the shader would disagree.
    public static SunLight? GetShadowLight () {
        SunLight? light = MainLight ?? (0 < SunLights.Count ? SunLights[0] : null);
        return light is not null && light.Enabled ? light : null;
    }

    public static void SetMainSunLight (SunLight sun) {
        //if (!LightSources.Contains(sun)) return;
        LightSources.Remove(sun);
        LightSources.Insert(0, sun);
    }


    public static void SetSceneUniformsLit (Shader shader) {
        if (Renderer.Instance.Camera is null) return;
        if (!shader.isLit) return;

        List<SunLight> enabledLights = SunLights.Where(l => l.Enabled).ToList();
        int count = Math.Min(enabledLights.Count, SunLights_Max);

        SunLight? shadowLight = GetShadowLight();
        int shadowIndex = shadowLight is null ? -1 : enabledLights.IndexOf(shadowLight);
        if (count <= shadowIndex) shadowIndex = -1; /// cut off by SunLights_Max
        shader.SetInt("uShadowLightIndex", shadowIndex);

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

            shader.SetVector3Array(uSunLightDir, dirs);
            shader.SetVector3Array(uSunLightColor, colors);
            shader.SetFloatArray(uSunLightIntensity, intensities);
        }
        shader.SetInt(uSunLightCount, count);

        List<PointLight> enabledPointLights = PointLights.Where(l => l.Enabled).ToList();
        int pointCount = Math.Min(enabledPointLights.Count, PoinyLights_Max);
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

            shader.SetVector3Array(uPointLightColor, colors);
            shader.SetFloatArray(uPointLightIntensity, intensities);
            shader.SetVector3Array(uPointLightPos, positions);
            shader.SetFloatArray(uPointLightRange, ranges);
        }
        shader.SetInt(uPointLightCount, pointCount);

        /// General
        shader.SetVector3(uAmbientColor, Constants.Ambient_Color);
        shader.SetFloat(uAmbientColorIntensity, Constants.Ambient_Intensity);
        shader.SetFloat(uExposure, Renderer.Instance.Camera.Exposure);

        /// Shadows
        ShadowMap? shadow = Renderer.Instance.Shadow;
        if (shadow is not null) {
            shadow.Depth.Bind(TextureUnit.Texture5);
            shader.SetInt(Shader.uShadowMap, 5);
            shader.SetMatrix4x4(Shader.uLightSpace, shadow.LightSpace);
            shader.SetFloat(Shader.uShadowTexelWorld, shadow.TexelWorld);
            shader.SetFloat(Shader.uShadowSoftness, 1.0f);
            Renderer.GL.ActiveTexture(TextureUnit.Texture0); /// Texture.Bind leaves unit 5 active; restore so later code is unaffected
        }

        if (Constants.renderSkyboxReflection)
            shader.SetFloat(uReflectionIntensity, Constants.reflectionIntensity);
    }

    public static void SetSHAmbient (Shader shader, in SHAmbientProbe probe) {
        shader.SetVector4(uSHAr, probe.SHAr);
        shader.SetVector4(uSHAg, probe.SHAg);
        shader.SetVector4(uSHAb, probe.SHAb);
        shader.SetVector4(uSHBr, probe.SHBr);
        shader.SetVector4(uSHBg, probe.SHBg);
        shader.SetVector4(uSHBb, probe.SHBb);
        shader.SetVector4(uSHC, probe.SHC);
        shader.SetFloat(uAmbientColorIntensity, probe.Intensity);
    }


}
