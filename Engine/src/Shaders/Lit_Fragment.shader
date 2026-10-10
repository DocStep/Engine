#version 400 core

// Lit_Fragment.shader — PBR (Cook-Torrance / GGX / Fresnel-Schlick)
// Multiple directional sun lights + SH ambient (diffuse) + equirect skybox reflection (specular).

#define MAX_POINT_SHADOWS 4


in vec3 vNormal;
in vec3 vFragPos;
in vec2 vUV;
in vec3 vViewNormal;
// flat in int vInstanceId;

#define MAX_SUN_LIGHTS 32
#define MAX_POINT_LIGHTS 32

const float PI = 3.14159265;

uniform vec3 uColor = vec3(1.0);
uniform sampler2D uTexture;
uniform int uHasTexture = 0;
uniform float uSmoothness = 0.5;
uniform float uMetallic = 0.0;
uniform float uAlpha = 1.0;

uniform int uSunLightCount = 0;
uniform vec3 uSunLightColor[MAX_SUN_LIGHTS];
uniform float uSunLightIntensity[MAX_SUN_LIGHTS];
uniform vec3 uSunLightDir[MAX_SUN_LIGHTS]; // direction light TRAVELS (sun -> scene)

uniform int uPointLightCount = 0;
uniform vec3 uPointLightPos[MAX_POINT_LIGHTS];
uniform vec3 uPointLightColor[MAX_POINT_LIGHTS];
uniform float uPointLightIntensity[MAX_POINT_LIGHTS];
uniform float uPointLightRange[MAX_POINT_LIGHTS];

const float SUN_SOFTNESS = 1.5;
const float POINT_SOFTNESS = 0.004;  // jitter relative to distance
const float POINT_BIAS = 0.001;      // in range-normalized depth

uniform float uSunShadow[MAX_SUN_LIGHTS];         // 0 = off, 1 = full strength (fractions fade it)
uniform float uPointShadowSlot[MAX_POINT_LIGHTS]; // -1 = off, else cube slot
uniform samplerCubeArray uPointShadowMap;

uniform sampler2DShadow uShadowMap;   // was sampler2D
uniform mat4 uLightSpace;
uniform float uShadowTexelWorld;
uniform float uShadowBias = 0.0;
uniform float uShadowNormalOffset = 3.0;
//uniform float uShadowAmbientDiffuse = 0.0;
//uniform float uShadowAmbientSpecular = 0.0; // 0 = off, 1 = reflection fully black in shadow

uniform vec3 uViewPos;

uniform float uEnvIntensity = 1.0;

uniform sampler2D uSkybox; // equirectangular; mip chain = pre-blurred roughness levels
uniform float uMaxReflectionLod = 1.0;
uniform float uReflectionIntensity = 1.0;

uniform int uAmbientMode = 2; // 0 = color, 1 = gradient, 2 = skybox (SH)
uniform vec3 uAmbientColor = vec3(0.2);
uniform vec3 uAmbientTop = vec3(0.5, 0.7, 1.0);
uniform vec3 uAmbientMiddle = vec3(0.4);
uniform vec3 uAmbientBottom = vec3(0.15);
uniform float uAmbientIntensity = 1.0;

// L2 spherical harmonics ambient
uniform vec4 uSHAr;
uniform vec4 uSHAg;
uniform vec4 uSHAb;
uniform vec4 uSHBr;
uniform vec4 uSHBg;
uniform vec4 uSHBb;
uniform vec4 uSHC;


layout (location = 0) out vec4 FragColor;
layout (location = 1) out vec4 FragNormal;


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

vec3 SampleAmbient(vec3 N)
{
    if (uAmbientMode == 0) return uAmbientColor*uAmbientIntensity;
    if (uAmbientMode == 1) {
        vec3 g = 0.0 < N.y ? mix(uAmbientMiddle, uAmbientTop, N.y) : mix(uAmbientMiddle, uAmbientBottom, -N.y);
        return g*uAmbientIntensity;
    }
    return SampleIrradianceSH(N)*uAmbientIntensity;
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
    return (kD * albedo + specular) * radiance * NdotL;
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
    return (kD * albedo + specular) * radiance * NdotL;
}

// 0 = lit, 1 = fully in shadow. N and L are used only for slope-scaled bias.
float ShadowFactor (vec3 N, vec3 L)
{
    float NdotL = clamp(dot(N, L), 0.0, 1.0);
    float sinT = sqrt(1.0 - NdotL*NdotL);
    float tanT = min(sinT/max(NdotL, 0.05), 10.0);

    /// never zero, so sun-facing surfaces are pushed off too
    vec3 pos = vFragPos + N*uShadowTexelWorld*uShadowNormalOffset*(0.5 + sinT);
    vec4 lp = uLightSpace*vec4(pos, 1.0);
    vec3 p = (lp.xyz/lp.w)*0.5 + 0.5;

    if (p.z < 0.0 || 1.0 < p.z) return 0.0;
    vec2 edge = min(p.xy, 1.0 - p.xy);
    float edgeMin = min(edge.x, edge.y);
    if (edgeMin < 0.0) return 0.0;
    float fade = smoothstep(0.0, 0.1, edgeMin);

    float z = p.z - uShadowBias*(1.0 + tanT);
    vec2 t = 2.0/vec2(textureSize(uShadowMap, 0));

    float s = texture(uShadowMap, vec3(p.xy + vec2(-1, -1)*t, z))
            + texture(uShadowMap, vec3(p.xy + vec2( 1, -1)*t, z))
            + texture(uShadowMap, vec3(p.xy + vec2(-1,  1)*t, z))
            + texture(uShadowMap, vec3(p.xy + vec2( 1,  1)*t, z));
    if (3.99 < s) return 0.0;
    if (s < 0.01) return fade;

    s += texture(uShadowMap, vec3(p.xy, z))
       + texture(uShadowMap, vec3(p.xy + vec2( 1, 0)*t, z))
       + texture(uShadowMap, vec3(p.xy + vec2(-1, 0)*t, z))
       + texture(uShadowMap, vec3(p.xy + vec2(0,  1)*t, z))
       + texture(uShadowMap, vec3(p.xy + vec2(0, -1)*t, z));
    return (1.0 - s/9.0)*fade;
}

// const vec3 kDisk[20] = vec3[](
//     vec3(1,1,1), vec3(1,-1,1), vec3(-1,-1,1), vec3(-1,1,1),
//     vec3(1,1,-1), vec3(1,-1,-1), vec3(-1,-1,-1), vec3(-1,1,-1),
//     vec3(1,1,0), vec3(1,-1,0), vec3(-1,-1,0), vec3(-1,1,0),
//     vec3(1,0,1), vec3(-1,0,1), vec3(1,0,-1), vec3(-1,0,-1),
//     vec3(0,1,1), vec3(0,-1,1), vec3(0,-1,-1), vec3(0,1,-1));

const vec3 kDisk[8] = vec3[](
    vec3(1,1,1), vec3(1,-1,1), vec3(-1,-1,1), vec3(-1,1,1),
    vec3(1,1,-1), vec3(1,-1,-1), vec3(-1,-1,-1), vec3(-1,1,-1));

float PointShadowFactor (int slot, vec3 lightPos, float range, vec3 N)
{
    vec3 toLight = lightPos - vFragPos;
    float dist = length(toLight);
    if (range <= dist) return 0.0;

    float NdotL = clamp(dot(N, toLight/dist), 0.0, 1.0);
    vec3 dir = (vFragPos + N*dist*0.006*(1.0 - NdotL)) - lightPos; /// normal offset grows with distance, like a texel does
    float current = length(dir)/range;

    float shadow = 0.0;
    for (int i = 0; i < 8; i++) {
        float d = texture(uPointShadowMap, vec4(dir + kDisk[i]*dist*POINT_SOFTNESS, float(slot))).r;
        if (current - POINT_BIAS > d) shadow += 1.0;
    }
    return shadow/8.0;
}


void main() {
    vec3 albedo = uColor;
    float metallic = clamp(uMetallic, 0.0, 1.0);
    float roughness = clamp(1.0 - uSmoothness, 0.045, 1.0); // smoothness -> roughness, avoid 0-roughness singularity
    float alpha = uAlpha;
    
    if (uHasTexture == 1) {
        // vec4 texSample = texture(uTexture, vUV);
        vec4 texSample = textureLod(uTexture, vUV, 0.0);
        albedo *= texSample.rgb;
        alpha *= texSample.a;
    }

    vec3 N = normalize(vNormal);
    vec3 V = normalize(uViewPos - vFragPos);

    vec3 F0 = mix(vec3(0.04), albedo, metallic);

    vec3 R = reflect(-V, N);

    // Direct sun lights — Cook-Torrance specular + Lambert diffuse, summed
    float shadow = 0.0;
    float shadowFacing = 0.0;
    vec3 Lo = vec3(0.0);
    for (int i = 0; i < uSunLightCount; i++) {
        vec3 sun = ComputeSunLight(i, N, V, F0, roughness, albedo, metallic);
        vec3 Ls = normalize(-uSunLightDir[i]);
        float facing = dot(N, Ls);
        if (0.0 < uSunShadow[i]) {
            if (0.0 < facing) {
                float s = ShadowFactor(N, Ls)*uSunShadow[i];
                sun *= 1.0 - s;
                shadow = max(shadow, s);
                shadowFacing = max(shadowFacing, smoothstep(0.0, 0.25, facing));
            } else {
                shadow = max(shadow, uSunShadow[i]);
                shadowFacing = 1.0;
            }
        }
        Lo += sun;
    }
    for (int i = 0; i < uPointLightCount; i++) {
        vec3 pl = ComputePointLight(i, N, V, F0, roughness, albedo, metallic);
        if (0.0 <= uPointShadowSlot[i]) {
            pl *= 1.0 - PointShadowFactor(int(uPointShadowSlot[i]), uPointLightPos[i], uPointLightRange[i], N);
        }
        Lo += pl;
    }

    float ambientShadow = shadow*shadowFacing;

    // IBL ambient — SH diffuse (Fresnel-split, energy-conserving) + prefiltered skybox specular
    vec3 Fr = FresnelSchlickRoughness(max(dot(N, V), 0.0), F0, roughness);
    vec3 kD_ambient = (vec3(1.0) - Fr) * (1.0 - metallic);

    vec3 irradiance = SampleAmbient(N);
    vec3 ambientDiffuse = irradiance * albedo * kD_ambient;

    vec3 prefiltered = textureLod(uSkybox, SampleSphericalMap(R), roughness*uMaxReflectionLod).rgb;
    vec3 ambientSpecular = prefiltered*Fr*uReflectionIntensity;
    // ambientSpecular = (1.0 - shadow)*ambientSpecular;

    vec3 color = ambientDiffuse + ambientSpecular + Lo;
    
    // FragColor = vec4(vNormal01, 1);
    FragColor = vec4(color, 1);
    FragNormal = vec4(vViewNormal, 1.0); // write view-space normal to MRT attachment 1
}
