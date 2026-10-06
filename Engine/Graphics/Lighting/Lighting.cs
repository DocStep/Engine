using System.Linq;
using Silk.NET.OpenGL;
using static Engine.Graphics.Shader;

namespace Engine.Graphics;


public static class Lighting {

    [Hide] public static List<LightSource> LightSources = new List<LightSource>();

    [Hide] public static List<SunLight> SunLights = new List<SunLight>();
    [Hide] public static List<PointLight> PointLights = new List<PointLight>();

    [Hide] public static SunLight? mainLight ;
    [Hide] public static SunLight? MainLight {
        get => 0 < SunLights.Count ? SunLights[0] : null;
        set {
            LightSources.Remove(value!);
            LightSources.Insert(0, value!);
            SunLights.Remove(value!);
            SunLights.Insert(0, value!);
        }
    }

    public static bool UseShadow = true;
    [Hide, Newtonsoft.Json.JsonIgnore] public static int ShadowLayer = -1; /// set every frame by Lighting.AssignShadowLayers
    [Range(0, int.MaxValue)] public static int SunLights_Max = 32;
    [Range(0, int.MaxValue)] public static int PointLights_Max = 32;



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

    /// Lighting
    public static SunLight? GetShadowSun () {
        if (!UseShadow) return null;
        for (int i = 0; i < SunLights.Count; i++) {
            SunLight l = SunLights[i];
            if (l.Enabled && l.CastShadows) return l;
        }
        return null;
    }

    /// Fills slots with the nearest shadow-casting point lights (null = unused)
    public static void PickPointShadowLights (PointLight?[] slots, Vector3 camPos) {
        Array.Clear(slots);
        if (!UseShadow) return;

        for (int s = 0; s < slots.Length; s++) {
            PointLight? best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < PointLights.Count; i++) {
                PointLight l = PointLights[i];
                if (!l.Enabled || !l.CastShadows) continue;
                if (0 <= Array.IndexOf(slots, l)) continue;
                float d = Vector3.DistanceSquared(l.Position, camPos);
                if (d < bestD) { bestD = d; best = l; }
            }
            slots[s] = best;
        }
    }

    public static void SetMainSunLight (SunLight sun) {
        //if (!LightSources.Contains(sun)) return;
        LightSources.Remove(sun);
        LightSources.Insert(0, sun);
    }


    public static void SetSceneUniformsLit (Shader shader) {
        Renderer renderer = Renderer.Instance;
        if (renderer.Camera is null) return;
        if (!shader.isLit) return;

        List<SunLight> enabledLights = SunLights.Where(l => l.Enabled).ToList();
        int count = Math.Min(enabledLights.Count, SunLights_Max);
        float sunLightIntensityAvg = 0;
        if (0 < count) {
            Vector3[] dirs = new Vector3[count];
            Vector3[] colors = new Vector3[count];
            float[] intensities = new float[count];
            float[] sunShadow = new float[count];

            for (int i = 0; i < count; i++) {
                SunLight light = enabledLights[i];
                dirs[i] = Mathf.QuaternionToDirection(light.Rotation);
                colors[i] = light.Color;
                intensities[i] = light.Intensity;
                sunShadow[i] = light == renderer.SunShadowLight ? 1f : 0f; /// only the sun that was really rendered
                sunLightIntensityAvg += light.Intensity;
            }
            sunLightIntensityAvg /= Math.Max(1, count);

            shader.SetVector3Array(uSunLightDir, dirs);
            shader.SetVector3Array(uSunLightColor, colors);
            shader.SetFloatArray(uSunLightIntensity, intensities);
            shader.SetFloatArray(uSunShadow, sunShadow);
        }
        shader.SetInt(uSunLightCount, count);

        List<PointLight> enabledPointLights = PointLights.Where(l => l.Enabled).ToList();
        int pointCount = Math.Min(enabledPointLights.Count, PointLights_Max);
        if (0 < pointCount) {
            Vector3[] positions = new Vector3[pointCount];
            Vector3[] colors = new Vector3[pointCount];
            float[] intensities = new float[pointCount];
            float[] ranges = new float[pointCount];
            float[] pointSlot = new float[pointCount];

            for (int i = 0; i < pointCount; i++) {
                PointLight light = enabledPointLights[i];
                positions[i] = light.Position;
                colors[i] = light.Color;
                intensities[i] = light.Intensity;
                ranges[i] = light.Range;
                pointSlot[i] = Array.IndexOf(renderer.PointShadows.Slots, light); /// -1 = no cube for this light
            }

            shader.SetVector3Array(uPointLightColor, colors);
            shader.SetFloatArray(uPointLightIntensity, intensities);
            shader.SetVector3Array(uPointLightPos, positions);
            shader.SetFloatArray(uPointLightRange, ranges);
            shader.SetFloatArray(uPointShadowSlot, pointSlot);
        }
        shader.SetInt(uPointLightCount, pointCount);

        /// General
        shader.SetFloat(uEnvIntensity, sunLightIntensityAvg);
        //shader.SetVector3(uAmbientColor, Constants.Ambient_Color);
        //shader.SetFloat(uAmbientColorIntensity, Constants.Ambient_Intensity);

        /// Shadows: always bound, even when off, so the samplers never fall back to unit 0 (skybox)
        renderer.Shadow.Bind(TextureUnit.Texture5);
        shader.SetInt(uShadowMap, 5);
        shader.SetMatrix4x4(uLightSpace, renderer.Shadow.LightSpace);
        shader.SetFloat(uShadowTexelWorld, renderer.Shadow.TexelWorld);
        shader.SetFloat(uShadowBias, renderer.Shadow.Bias);
        //shader.SetFloat(uShadowNormalOffset, 3.0f);
        //shader.SetFloat(uShadowAmbientSpecular, 0.6f);
        //shader.SetFloat(uShadowAmbientDiffuse, 0.1f);

        renderer.PointShadows.Bind(TextureUnit.Texture6);
        shader.SetInt(uPointShadowMap, 6);
        Renderer.GL.ActiveTexture(TextureUnit.Texture0);

        /// metal reflection: 0 with no sun, capped at 1 (test hack, replace with time of day later)
        float envReflection = Math.Clamp(sunLightIntensityAvg, 0f, 1f);
        shader.SetFloat(uReflectionIntensity, Constants.renderSkyboxReflection ? Constants.reflectionIntensity*envReflection : 0f);
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
