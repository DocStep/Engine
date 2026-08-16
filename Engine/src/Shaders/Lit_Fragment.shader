#version 330 core

in vec3 vNormal;
in vec3 vFragPos;

uniform vec3 uColor;
uniform float uSmoothness;
uniform float uMetallic;

#define MAX_SUN_LIGHTS 32
uniform int uSunLightCount;
uniform vec3 uSunLightColor[MAX_SUN_LIGHTS];
uniform float uSunLightIntensity[MAX_SUN_LIGHTS];
uniform vec3 uSunLightDir[MAX_SUN_LIGHTS];

uniform vec3 uViewPos;

uniform vec3 uAmbientColor;
uniform float uAmbientColorIntensity;
uniform sampler2D uSkybox;
uniform float uMaxReflectionLod;
uniform float uReflectionIntensity;

const float PI = 3.14159265;
const float uExposure = 1.0;

out vec4 FragColor;

float D_GGX (float NdH, float a2) {
    float d = NdH*NdH*(a2 - 1.0) + 1.0;
    return a2/(PI*d*d + 1e-4);
}
float G1 (float NdX, float k) { return NdX/(NdX*(1.0 - k) + k + 1e-4); }
float G_Smith (float NdV, float NdL, float rough) {
    float r = rough + 1.0, k = r*r/8.0;
    return G1(NdV, k)*G1(NdL, k);
}
vec3 F_Schlick (float cosTheta, vec3 F0) {
    return F0 + (1.0 - F0)*pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}
vec3 F_SchlickSmoothness (float cosTheta, vec3 F0, float Smoothness) {
    vec3 maxF0 = max(vec3(1.0 - Smoothness), F0);
    return F0 + (maxF0 - F0)*pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

float GetSpecularOcclusion (float NdV, float occlusion, float Smoothness) {
    return clamp(pow(NdV + occlusion, exp2(-16.0*Smoothness - 1.0)) - 1.0 + occlusion, 0.0, 1.0);
}

vec2 DirToEquirectUV (vec3 dir) {
    float u = 0.5 + atan(dir.z, dir.x)/(2.0*PI);
    float v = 0.5 + asin(clamp(dir.y, -1.0, 1.0))/PI;
    return vec2(u, v);
}

/// One directional light's contribution, given precomputed view-dependent terms
vec3 ComputeSunLight (int i, vec3 N, vec3 V, float NdV, vec3 F0, float rough, float a2, float smoothness) {
    vec3 L = normalize(-uSunLightDir[i]);
    vec3 H = normalize(L + V);

    float NdL = max(dot(N, L), 0.0);
    float NdH = max(dot(N, H), 0.0);
    float HdV = max(dot(H, V), 0.0);

    vec3 F = F_Schlick(HdV, F0);
    float D = D_GGX(NdH, a2);
    float G = G_Smith(NdV, NdL, rough);
    vec3 spec = (D*G*F)/(4.0*NdV*NdL + 1e-4);
    vec3 kD = (1.0 - F)*(1.0 - uMetallic);

    return (kD*uColor/PI + spec)*uSunLightColor[i]*uSunLightIntensity[i]*NdL;
}

void main () {
    vec3 N = normalize(vNormal);
    vec3 V = normalize(uViewPos - vFragPos);
    vec3 R = reflect(-V, N);

    float NdV = max(dot(N, V), 1e-4);

    float smoothness = clamp(uSmoothness, 0.0, 1.0);
    float rough = max(1.0 - smoothness, 0.04);
    float a = rough*rough;
    float a2 = max(a*a, 1e-3);

    vec3 F0 = mix(vec3(0.04), uColor, uMetallic);

    /// Direct light — sum over all suns
    vec3 Lo = vec3(0.0);
    for (int i = 0; i < uSunLightCount; i++) {
        Lo += ComputeSunLight(i, N, V, NdV, F0, rough, a2, smoothness);
    }

    /// --- Ambient diffuse ---
    vec3 Fambient = F_SchlickSmoothness(NdV, F0, smoothness);
    vec3 kDambient = (1.0 - Fambient)*(1.0 - uMetallic);
    vec3 diffuseAmbient = kDambient*uAmbientColorIntensity*uAmbientColor*uColor;

    /// Ambient specular (IBL)
    float lod = rough*uMaxReflectionLod;
    vec2 envUV = DirToEquirectUV(R);
    vec3 envSpec = textureLod(uSkybox, envUV, lod).rgb*uExposure;
    vec3 envSpecMixed = mix(uAmbientColor, envSpec, clamp(uReflectionIntensity, 0.0, 1.0));
    float specOcclusion = GetSpecularOcclusion(NdV, 1.0, smoothness);
    vec3 specularAmbient = envSpecMixed*Fambient*specOcclusion;

    vec3 ambient = diffuseAmbient + specularAmbient;
    vec3 color = ambient + Lo;

    /// Tonemapping
    color = color/(color + vec3(1.0));
    color = pow(color, vec3(1.0/2.2));

    FragColor = vec4(color, 1.0);
}