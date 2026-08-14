#version 330 core

// Lit_Fragment.shader — PBR (Cook-Torrance / GGX / Fresnel-Schlick)
// Directional sun light + flat ambient + equirect skybox reflection.

in vec3 vNormal;
in vec3 vFragPos;

uniform vec3 uColor;
uniform float uSmoothness;
uniform float uMetallic;
uniform vec3 uSunLightColor;
uniform float uSunLightIntensity;
uniform vec3 uSunLightDir;      // assumed: direction light TRAVELS (sun -> scene)
uniform vec3 uViewPos;

uniform vec3 uAmbientColor;
uniform float uAmbientColorIntensity;
uniform sampler2D uSkybox;      // equirectangular; mip chain = pre-blurred roughness levels
uniform float uMaxReflectionLod;
uniform float uReflectionIntensity;

const float PI = 3.14159265;
const float uExposure = 1.0;

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

void main()
{
    vec3 albedo = uColor;
    float metallic = clamp(uMetallic, 0.0, 1.0);
    float roughness = clamp(1.0 - uSmoothness, 0.045, 1.0); // smoothness -> roughness, avoid 0-roughness singularity

    vec3 N = normalize(vNormal);
    vec3 V = normalize(uViewPos - vFragPos);
    vec3 L = normalize(-uSunLightDir);
    vec3 H = normalize(V + L);

    vec3 F0 = mix(vec3(0.04), albedo, metallic);

    // Direct sun light — Cook-Torrance specular + Lambert diffuse
    float NDF = DistributionGGX(N, H, roughness);
    float G = GeometrySmith(N, V, L, roughness);
    vec3 F = FresnelSchlick(max(dot(H, V), 0.0), F0);

    vec3 specular = (NDF * G * F) /
        (4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 1e-4);
    vec3 kD = (vec3(1.0) - F) * (1.0 - metallic);

    float NdotL = max(dot(N, L), 0.0);
    vec3 radiance = uSunLightColor * uSunLightIntensity;
    vec3 Lo = (kD * albedo / PI + specular) * radiance * NdotL;

    // Flat ambient (diffuse) + skybox reflection (specular)
    vec3 ambientDiffuse = uAmbientColor * uAmbientColorIntensity * albedo * (1.0 - metallic);

    vec3 R = reflect(-V, N);
    vec3 prefiltered = textureLod(uSkybox, SampleSphericalMap(R), roughness * uMaxReflectionLod).rgb;
    vec3 Fr = FresnelSchlickRoughness(max(dot(N, V), 0.0), F0, roughness);
    vec3 ambientSpecular = prefiltered * Fr * uReflectionIntensity;

    vec3 color = ambientDiffuse + ambientSpecular + Lo;

    // Exposure + Reinhard tone map + gamma. Skip the final pow() if your
    // framebuffer is sRGB-enabled already, or you'll double-correct.
    color *= uExposure;
    color = color / (color + vec3(1.0));
    color = pow(color, vec3(1.0 / 2.2));

    FragColor = vec4(color, 1.0);
}