#version 400 core

#define MAX_POINT_SHADOWS 4


in vec3 vNormal;
in vec3 vFragPos;
in vec2 vUV;
in vec3 vViewNormal;
// flat in int vInstanceId;

#define MAX_SUN_LIGHTS 32
#define MAX_POINT_LIGHTS 32

const float PI = 3.14159265;
const float uSunAngularRadius = 0.0047; /// radians, smallest highlight size (real sun is ~0.0047)
const float uPointLightSourceRadius = 0.05; /// world units, smallest highlight size (bigger = bigger, softer highlight)

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

const float SUN_SOFTNESS = 6.0;
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
layout (location = 1) out vec3 FragNormal;

// ============================================================================
//  Helpers
// ============================================================================

float saturate(float x) { return clamp(x, 0.0, 1.0); }
float sq(float x)       { return x * x; }
float pow5(float x)     { float x2 = x * x; return x2 * x2 * x; }

/// GGX, precise cross-product form, alpha = roughness*roughness
float DistributionGGXSun(vec3 N, vec3 H, float alpha)
{
    float NdotH = clamp(dot(N, H), 0.0, 1.0);
    vec3 NxH = cross(N, H);
    float k = alpha / (dot(NxH, NxH) + NdotH*NdotH*alpha*alpha);
    return k*k / PI;
}

/// height-correlated Smith visibility (already includes 1/(4 NoL NoV))
float V_SmithGGXCorrelated(float NoV, float NoL, float a)
{
    float a2 = a * a;
    float gv = NoL * sqrt(NoV * NoV * (1.0 - a2) + a2);
    float gl = NoV * sqrt(NoL * NoL * (1.0 - a2) + a2);
    return 0.5 / (gv + gl + 1e-5);
}

vec3 FresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow5(clamp(1.0 - cosTheta, 0.0, 1.0));
}

/// Frostbite/Burley diffuse, energy-normalised, includes 1/PI
float Fd_Burley(float NoV, float NoL, float LoH, float rough)
{
    float energyBias   = mix(0.0, 0.5, rough);
    float energyFactor = mix(1.0, 1.0 / 1.51, rough);
    float f90 = energyBias + 2.0 * LoH * LoH * rough;
    float fl = 1.0 + (f90 - 1.0) * pow5(1.0 - NoL);
    float fv = 1.0 + (f90 - 1.0) * pow5(1.0 - NoV);
    return fl * fv * energyFactor / PI;
}

/// analytic split-sum approximation (Karis, mobile): returns (scale, bias) on F0
vec2 EnvBRDFApprox(float rough, float NoV)
{
    const vec4 c0 = vec4(-1.0, -0.0275, -0.572,  0.022);
    const vec4 c1 = vec4( 1.0,  0.0425,  1.040, -0.040);
    vec4 r = rough * c0 + c1;
    float a004 = min(r.x * r.x, exp2(-9.28 * NoV)) * r.x + r.y;
    return vec2(-1.04, 1.04) * a004 + r.zw;
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
    return SampleIrradianceSH(N)*uAmbientIntensity*uEnvIntensity;
}

// ============================================================================
//  Direct lights
// ============================================================================

/// shared BRDF eval. widen = area-light roughness widening (added to alpha).
vec3 ShadeLight(vec3 N, vec3 V, vec3 L, float NoV, float alpha, float widen,
                float roughness, vec3 F0, vec3 albedo, float metallic, vec3 comp)
{
    float NoL = saturate(dot(N, L));
    if (NoL <= 0.0) return vec3(0.0);

    vec3 H = normalize(V + L);
    float LoH = saturate(dot(L, H));
    float VoH = saturate(dot(V, H));

    float aW = clamp(alpha + widen, 1e-4, 1.0);
    float D  = DistributionGGXSun(N, H, aW) * sq(alpha / aW); // keep energy constant as the lobe widens
    float Vis = V_SmithGGXCorrelated(NoV, NoL, alpha);
    vec3  F  = FresnelSchlick(VoH, F0);

    vec3 specular = D * Vis * F * comp;
    vec3 kD = (vec3(1.0) - F) * (1.0 - metallic);
    vec3 diffuse = kD * albedo * Fd_Burley(NoV, NoL, LoH, roughness);

    return (diffuse + specular) * NoL;
}

vec3 ComputeSunLight(int i, vec3 N, vec3 V, float NoV, float alpha, vec3 F0,
                     float roughness, vec3 albedo, float metallic, vec3 comp)
{
    vec3 L = normalize(-uSunLightDir[i]);
    /// sun is a disc, not a point: widen the lobe by its size so the highlight stops shrinking
    vec3 c = ShadeLight(N, V, L, NoV, alpha, 0.5*uSunAngularRadius, roughness, F0, albedo, metallic, comp);
    return c * uSunLightColor[i] * uSunLightIntensity[i] * PI; // intensity 1.0 = white Lambert surface outputs 1.0
}

vec3 ComputePointLight(int i, vec3 N, vec3 V, float NoV, float alpha, vec3 F0,
                       float roughness, vec3 albedo, float metallic, vec3 comp)
{
    vec3 toLight = uPointLightPos[i] - vFragPos;
    float dist = length(toLight);
    vec3 L = toLight/max(dist, 1e-4);

    /// light is a small sphere, not a point: widen the lobe by its angular size
    float angularRadius = uPointLightSourceRadius/max(dist, 1e-4);

    /// Squared-distance falloff windowed to zero at uPointLightRange[i] (Karis-style)
    float range = max(uPointLightRange[i], 1e-4);
    float window = clamp(1.0 - pow(dist/range, 4.0), 0.0, 1.0);
    float falloff = (window*window)/(dist*dist + 1.0);

    vec3 c = ShadeLight(N, V, L, NoV, alpha, 0.5*angularRadius, roughness, F0, albedo, metallic, comp);
    return c * uPointLightColor[i]*uPointLightIntensity[i]*falloff*PI;
}

// ============================================================================
//  Shadows
// ============================================================================

/// world-position screen derivatives; set at the top of main() (uniform control flow) so
/// ShadowFactor can use them from inside divergent branches
vec3 gPosDx;
vec3 gPosDy;

const int   SUN_PCF_TAPS = 16;
const float RP_MAX_BIAS  = 0.003; // clamp for receiver-plane bias (normalised depth), tune to your depth range

float InterleavedGradientNoise(vec2 p)
{
    return fract(52.9829189*fract(dot(p, vec2(0.06711056, 0.00583715))));
}

vec2 VogelDisk(int i, int n, float phi)
{
    float r = sqrt((float(i) + 0.5)/float(n));
    float t = float(i)*2.39996323 + phi;
    return vec2(cos(t), sin(t))*r;
}

// 0 = lit, 1 = fully in shadow.
// Rotated Vogel-disk PCF (hardware 2x2 compare per tap) + receiver-plane depth bias:
// every tap compares against the depth of the receiver's own plane at that tap, so the
// kernel no longer self-shadows on slopes and no big slope-scaled bias is needed.
float ShadowFactor (vec3 N, vec3 L) {
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
    vec2 texel = 1.0/vec2(textureSize(uShadowMap, 0));
    float phi = 6.2831853*InterleavedGradientNoise(gl_FragCoord.xy);

    float lit = 0.0;
    for (int k = 0; k < SUN_PCF_TAPS; ++k) {
        vec2 o = VogelDisk(k, SUN_PCF_TAPS, phi)*SUN_SOFTNESS*texel;
        lit += textureLod(uShadowMap, vec3(p.xy + o, z), 0.0);
    }
    return (1.0 - lit/float(SUN_PCF_TAPS))*fade;
}

const int   POINT_PCF_TAPS = 12;
const float POINT_NORMAL_OFFSET = 2.0; // texels
const float POINT_MAX_TAN = 8.0;       // clamp for the slope term at grazing angles

// 0 = lit, 1 = fully in shadow.
// Normal offset and slope bias are both sized from the cube texel's world footprint at this
// distance, and the PCF kernel is a per-pixel rotated disk in the plane perpendicular to the
// sample direction (no fixed axis-aligned tap pattern).
float PointShadowFactor (int slot, vec3 lightPos, float range, vec3 N)
{
    vec3 toLight = lightPos - vFragPos;
    float dist = length(toLight);
    if (range <= dist) return 0.0;

    float NdotL = clamp(dot(N, toLight/dist), 0.0, 1.0);
    float sinT = sqrt(1.0 - NdotL*NdotL);
    float tanT = min(sinT/max(NdotL, 1e-3), POINT_MAX_TAN);

    float texelWorld = 2.0*dist/float(textureSize(uPointShadowMap, 0).x); // world size of one cube texel here
    float radius = max(POINT_SOFTNESS*dist, 1.5*texelWorld);              // PCF radius, world units

    vec3 pos = vFragPos + N*texelWorld*POINT_NORMAL_OFFSET*sinT;
    vec3 dir = pos - lightPos;
    float len = length(dir);
    float current = (len - (radius + 0.5*texelWorld)*tanT)/range - POINT_BIAS; // receiver-plane style slope bias

    vec3 fwd = dir/len;
    vec3 up = abs(fwd.y) < 0.99 ? vec3(0.0, 1.0, 0.0) : vec3(1.0, 0.0, 0.0);
    vec3 t = normalize(cross(up, fwd));
    vec3 b = cross(fwd, t);
    float phi = 6.2831853*InterleavedGradientNoise(gl_FragCoord.xy + 17.0);

    float shadow = 0.0;
    for (int i = 0; i < POINT_PCF_TAPS; i++) {
        vec2 o = VogelDisk(i, POINT_PCF_TAPS, phi)*radius;
        float d = textureLod(uPointShadowMap, vec4(dir + t*o.x + b*o.y, float(slot)), 0.0).r;
        if (current > d) shadow += 1.0;
    }
    return shadow/float(POINT_PCF_TAPS);
}

// ============================================================================
//  Main
// ============================================================================

void main() {
    gPosDx = dFdx(vFragPos); // must stay in uniform control flow
    gPosDy = dFdy(vFragPos);

    vec3 albedo = uColor;
    float metallic = clamp(uMetallic, 0.0, 1.0);
    float roughness = clamp(1.0 - uSmoothness, 0.045, 1.0); // smoothness -> roughness, avoid 0-roughness singularity
    float alpha = uAlpha;

    if (uHasTexture == 1) {
        vec4 texSample = texture(uTexture, vUV); // was textureLod(.., 0.0): swap back if you want mip 0 only
        albedo *= texSample.rgb;
        alpha *= texSample.a;
    }

    vec3 N = normalize(vNormal);
    if (!gl_FrontFacing) N = -N;
    vec3 V = normalize(uViewPos - vFragPos);
    float NoV = max(dot(N, V), 1e-4);

    vec3 F0 = mix(vec3(0.04), albedo, metallic);

    // GGX alpha with specular anti-aliasing: widen the lobe where the normal varies fast
    float a = roughness*roughness;
    {
        vec3 dndx = dFdx(N);
        vec3 dndy = dFdy(N);
        float variance = 0.25*(dot(dndx, dndx) + dot(dndy, dndy));
        a = sqrt(saturate(a*a + min(2.0*variance, 0.18)));
    }

    // split-sum env BRDF + multi-scatter energy compensation
    vec2 ab = EnvBRDFApprox(roughness, NoV);
    vec3 envF = F0*ab.x + ab.y;
    vec3 comp = vec3(1.0) + F0*(1.0/max(ab.x + ab.y, 1e-3) - 1.0);

    // Direct lights — each sun with uSunShadow > 0 samples the shadow map (as before)
    vec3 Lo = vec3(0.0);
    for (int i = 0; i < uSunLightCount; i++) {
        vec3 sun = ComputeSunLight(i, N, V, NoV, a, F0, roughness, albedo, metallic, comp);
        if (0.0 < uSunShadow[i]) {
            vec3 Ls = normalize(-uSunLightDir[i]);
            if (0.0 < dot(N, Ls))
                sun *= 1.0 - ShadowFactor(N, Ls)*uSunShadow[i];
        }
        Lo += sun;
    }
    for (int i = 0; i < uPointLightCount; i++) {
        vec3 pl = ComputePointLight(i, N, V, NoV, a, F0, roughness, albedo, metallic, comp);
        if (0.0 <= uPointShadowSlot[i]) {
            pl *= 1.0 - PointShadowFactor(int(uPointShadowSlot[i]), uPointLightPos[i], uPointLightRange[i], N);
        }
        Lo += pl;
    }

    // IBL ambient — SH diffuse + prefiltered skybox specular
    vec3 kD_ambient = (vec3(1.0) - envF) * (1.0 - metallic);
    vec3 ambientDiffuse = SampleAmbient(N) * albedo * kD_ambient;

    vec3 R = reflect(-V, N);
    {   // dominant specular direction (Lagarde & de Rousiers)
        float aR = roughness*roughness;
        R = normalize(mix(N, R, (1.0 - aR)*(sqrt(1.0 - aR) + aR)));
    }
    vec3 prefiltered = textureLod(uSkybox, SampleSphericalMap(R), roughness*uMaxReflectionLod).rgb;

    float horizon = saturate(1.0 + dot(R, N));
    horizon *= horizon;
    vec3 ambientSpecular = prefiltered * envF * comp * horizon * uReflectionIntensity * uEnvIntensity;

    vec3 color = ambientDiffuse + ambientSpecular + Lo;

    FragColor = vec4(max(color, vec3(0.0)), alpha);
    vec3 viewN = normalize(vViewNormal);
    FragNormal = gl_FrontFacing ? viewN : -viewN; // view-space normal, MRT attachment 1
}
