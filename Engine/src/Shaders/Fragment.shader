#version 330 core

in vec3 vNormal;
in vec3 vFragPos;
out vec4 FragColor;

uniform vec3 uColor;
uniform float uRoughness;
uniform float uMetallic;
uniform vec3 uSunLightColor;
uniform float uSunLightIntensity;
uniform vec3 uSunLightDir;
uniform vec3 uViewPos;

/// Revisit with real irradiance/prefilter maps later.
uniform vec3 uAmbientColor;

const float PI = 3.14159265;

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

void main () {
    vec3 N = normalize(vNormal);
    vec3 L = normalize(-uSunLightDir);
    vec3 V = normalize(uViewPos - vFragPos);
    vec3 H = normalize(L + V);

    float NdL = max(dot(N, L), 0.0);
    float NdV = max(dot(N, V), 0.0);
    float NdH = max(dot(N, H), 0.0);
    float HdV = max(dot(H, V), 0.0);

    float rough = max(uRoughness, 0.04);
    float a2 = rough*rough*rough*rough;

    vec3 F0 = mix(vec3(0.04), uColor, uMetallic);

    /// --- Direct light ---
    vec3 F = F_Schlick(HdV, F0);
    float D = D_GGX(NdH, a2);
    float G = G_Smith(NdV, NdL, rough);
    vec3 spec = (D*G*F)/(4.0*NdV*NdL + 1e-4);
    vec3 kD = (1.0 - F)*(1.0 - uMetallic);
    vec3 Lo = (kD*uColor/PI + spec)*uSunLightColor*uSunLightIntensity*NdL;

    /// --- Ambient (flat, placeholder) ---
    vec3 ambient = uAmbientColor*uColor*(1.0 - uMetallic);

    vec3 color = ambient + Lo;
    color = color/(color + vec3(1.0));   /// Reinhard tone map
    color = pow(color, vec3(1.0/2.2));   /// gamma correction

    FragColor = vec4(color, 1.0);
}
