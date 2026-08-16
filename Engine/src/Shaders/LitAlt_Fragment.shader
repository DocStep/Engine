#version 330 core

// Lit_Fragment.shader — PBR (Cook-Torrance / GGX / Fresnel-Schlick)
// Multiple directional sun lights + SH ambient (diffuse) + equirect skybox reflection (specular).

in vec3 vNormal;
in vec3 vFragPos;

#define MAX_SUN_LIGHTS 32
#define MAX_POINT_LIGHTS 32

uniform vec3 uColor;
uniform float uSmoothness;
uniform float uMetallic;

uniform int uSunLightCount;
uniform vec3 uSunLightColor[MAX_SUN_LIGHTS];
uniform float uSunLightIntensity[MAX_SUN_LIGHTS];
uniform vec3 uSunLightDir[MAX_SUN_LIGHTS];      // direction light TRAVELS (sun -> scene)

uniform int uPointLightCount;
uniform vec3 uPointLightPos[MAX_POINT_LIGHTS];
uniform vec3 uPointLightColor[MAX_POINT_LIGHTS];
uniform float uPointLightIntensity[MAX_POINT_LIGHTS];
uniform float uPointLightRange[MAX_POINT_LIGHTS];

uniform vec3 uViewPos;

// L2 spherical harmonics ambient (7 constants per Unity-style packing)
uniform vec4 uSHAr;
uniform vec4 uSHAg;
uniform vec4 uSHAb;
uniform vec4 uSHBr;
uniform vec4 uSHBg;
uniform vec4 uSHBb;
uniform vec4 uSHC;
uniform float uAmbientIntensity;

uniform sampler2D uSkybox;      // equirectangular; mip chain = pre-blurred roughness levels
uniform float uMaxReflectionLod;
uniform float uReflectionIntensity;

uniform float uExposure;

const float PI = 3.14159265;

out vec4 FragColor;

float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;
    float denom = NdotH2 * (a2 - 1.0) + 1.0;
    return a2 / (PI * denom * denom + 1e-7);
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = roughness + 1.0;
    float k = (r * r) / 8.0;
    return NdotV / (NdotV * (1.0 - k) + k);
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    return GeometrySchlickGGX(NdotV, roughness) * GeometrySchlickGGX(NdotL, roughness);
}

vec3 FresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

vec3 FresnelSchlickRoughness(float cosTheta, vec3 F0, float roughness)
{
    return F0 + (max(vec3(1.0 - roughness), F0) - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

const vec2 invAtan = vec2(0.1591, 0.3183);
vec2 SampleSphericalMap(vec3 v)
{
    vec2 uv = vec2(atan(v.z, v.x), asin(v.y));
    uv *= invAtan;
    uv += 0.5;
    return uv;
}

// L2 SH irradiance evaluation — N must be normalized.
// Encodes the pre-integrated Lambertian irradiance environment map
// (Ramamoorthi & Hanrahan), packed as 7 vec4/vec3 constants per Unity's convention.
vec3 SampleIrradianceSH(vec3 N)
{
    vec4 n = vec4(N, 1.0);

    vec3 x1;
    x1.r = dot(uSHAr, n);
    x1.g = dot(uSHAg, n);
    x1.b = dot(uSHAb, n);

    vec4 vB = n.xyzz * n.yzzx;
    vec3 x2;
    x2.r = dot(uSHBr, vB);
    x2.g = dot(uSHBg, vB);
    x2.b = dot(uSHBb, vB);

    float vC = N.x * N.x - N.y * N.y;
    vec3 x3 = uSHC.rgb * vC;

    return max(x1 + x2 + x3, vec3(0.0));
}

// One directional light's contribution — needs N, V, F0, roughness shared across lights
vec3 ComputeSunLight(int i, vec3 N, vec3 V, vec3 F0, float roughness, vec3 albedo, float metallic)
{
    vec3 L = normalize(-uSunLightDir[i]);
    vec3 H = normalize(V + L);

    float NDF = DistributionGGX(N, H, roughness);
    float G = GeometrySmith(N, V, L, roughness);
    vec3 F = FresnelSchlick(max(dot(H, V), 0.0), F0);

    vec3 specular = (NDF * G * F) /
        (4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 1e-4);
    vec3 kD = (vec3(1.0) - F) * (1.0 - metallic);

    float NdotL = max(dot(N, L), 0.0);
    vec3 radiance = uSunLightColor[i] * uSunLightIntensity[i];
    return (kD * albedo / PI + specular) * radiance * NdotL;
}
// One point light's contribution — position-based L, windowed inverse-square falloff
vec3 ComputePointLight(int i, vec3 N, vec3 V, vec3 F0, float roughness, vec3 albedo, float metallic)
{
    vec3 toLight = uPointLightPos[i] - vFragPos;
    float dist = length(toLight);
    vec3 L = toLight / max(dist, 1e-4);
    vec3 H = normalize(V + L);

    float NDF = DistributionGGX(N, H, roughness);
    float G = GeometrySmith(N, V, L, roughness);
    vec3 F = FresnelSchlick(max(dot(H, V), 0.0), F0);

    vec3 specular = (NDF * G * F) /
        (4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 1e-4);
    vec3 kD = (vec3(1.0) - F) * (1.0 - metallic);

    // Squared-distance falloff windowed to zero at uPointLightRange[i] (Karis-style)
    float range = max(uPointLightRange[i], 1e-4);
    float window = clamp(1.0 - pow(dist / range, 4.0), 0.0, 1.0);
    float falloff = (window * window) / (dist * dist + 1.0);

    float NdotL = max(dot(N, L), 0.0);
    vec3 radiance = uPointLightColor[i] * uPointLightIntensity[i] * falloff;
    return (kD * albedo / PI + specular) * radiance * NdotL;
}

void main()
{
    vec3 albedo = uColor;
    float metallic = clamp(uMetallic, 0.0, 1.0);
    float roughness = clamp(1.0 - uSmoothness, 0.045, 1.0); // smoothness -> roughness, avoid 0-roughness singularity

    vec3 N = normalize(vNormal);
    vec3 V = normalize(uViewPos - vFragPos);

    vec3 F0 = mix(vec3(0.04), albedo, metallic);

    // Direct sun lights — Cook-Torrance specular + Lambert diffuse, summed
    vec3 Lo = vec3(0.0);
    for (int i = 0; i < uSunLightCount; i++)
    {
        Lo += ComputeSunLight(i, N, V, F0, roughness, albedo, metallic);
    }
    for (int i = 0; i < uPointLightCount; i++)
    {
        Lo += ComputePointLight(i, N, V, F0, roughness, albedo, metallic);
    }

    // IBL ambient — SH diffuse (Fresnel-split, energy-conserving) + prefiltered skybox specular
    vec3 Fr = FresnelSchlickRoughness(max(dot(N, V), 0.0), F0, roughness);
    vec3 kD_ambient = (vec3(1.0) - Fr) * (1.0 - metallic);

    vec3 irradiance = SampleIrradianceSH(N) * uAmbientIntensity;
    vec3 ambientDiffuse = irradiance * albedo * kD_ambient;

    vec3 R = reflect(-V, N);
    vec3 prefiltered = textureLod(uSkybox, SampleSphericalMap(R), roughness * uMaxReflectionLod).rgb;
    vec3 ambientSpecular = prefiltered * Fr * uReflectionIntensity;

    vec3 color = ambientDiffuse + ambientSpecular + Lo;

    // Exposure + luminance-preserving Reinhard + gamma.
    // Tonemapping luminance (not per-channel) keeps hue/saturation intact at high intensity.
    // Skip the final pow() if your framebuffer is sRGB-enabled already, or you'll double-correct.
    color *= uExposure;

    float luminance = dot(color, vec3(0.2126, 0.7152, 0.0722));
    float toneMappedLuminance = luminance / (1.0 + luminance);
    color *= (luminance > 0.0) ? (toneMappedLuminance / luminance) : 0.0;

    color = pow(color, vec3(1.0 / 2.2));

    FragColor = vec4(color, 1.0);
}