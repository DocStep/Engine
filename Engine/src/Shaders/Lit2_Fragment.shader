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
vec3  saturate(vec3 x)  { return clamp(x, 0.0, 1.0); }
float sq(float x)       { return x * x; }
float pow5(float x)     { float x2 = x * x; return x2 * x2 * x; }

// Interleaved gradient noise (Jimenez) - cheap, stable per-pixel rotation/dither
float ign(vec2 p)
{
    return fract(52.9829189 * fract(dot(p, vec2(0.06711056, 0.00583715))));
}

vec2 vogelDisk(int i, int n, float phi)
{
    float r = sqrt((float(i) + 0.5) / float(n));
    float t = float(i) * 2.39996323 + phi;
    return vec2(cos(t), sin(t)) * r;
}

// ---------------------------------------------------------------- BRDF terms

float D_GGX(float NoH, float a)
{
    float a2 = a * a;
    float d = NoH * NoH * (a2 - 1.0) + 1.0;
    return a2 / (PI * d * d + 1e-7);
}

float V_SmithGGXCorrelated(float NoV, float NoL, float a)
{
    float a2 = a * a;
    float gv = NoL * sqrt(NoV * NoV * (1.0 - a2) + a2);
    float gl = NoV * sqrt(NoL * NoL * (1.0 - a2) + a2);
    return 0.5 / (gv + gl + 1e-5);
}

vec3 F_Schlick(vec3 F0, float VoH)
{
    return F0 + (vec3(1.0) - F0) * pow5(1.0 - VoH);
}

// Frostbite-style Burley diffuse (energy-normalised)
float Fd_Burley(float NoV, float NoL, float LoH, float rough)
{
    float energyBias   = mix(0.0, 0.5, rough);
    float energyFactor = mix(1.0, 1.0 / 1.51, rough);
    float f90 = energyBias + 2.0 * LoH * LoH * rough;
    float fl = 1.0 + (f90 - 1.0) * pow5(1.0 - NoL);
    float fv = 1.0 + (f90 - 1.0) * pow5(1.0 - NoV);
    return fl * fv * energyFactor / PI;
}

// Analytic split-sum approximation (Karis, mobile)
vec2 envBRDF(float rough, float NoV)
{
    const vec4 c0 = vec4(-1.0, -0.0275, -0.572,  0.022);
    const vec4 c1 = vec4( 1.0,  0.0425,  1.040, -0.040);
    vec4 r = rough * c0 + c1;
    float a004 = min(r.x * r.x, exp2(-9.28 * NoV)) * r.x + r.y;
    return vec2(-1.04, 1.04) * a004 + r.zw;
}

// One analytic light: diffuse + GGX spec. 'widen' = Karis area-light roughness widening.
vec3 shadeLight(vec3 N, vec3 V, vec3 L, float NoV, float alpha, float widen,
                float rough, vec3 F0, vec3 diffC, vec3 comp)
{
    float NoL = saturate(dot(N, L));
    if (NoL <= 0.0) return vec3(0.0);

    vec3 H = normalize(V + L);
    float NoH = saturate(dot(N, H));
    float LoH = saturate(dot(L, H));
    float VoH = saturate(dot(V, H));

    float aW = saturate(alpha + widen);
    float D  = D_GGX(NoH, aW) * sq(alpha / aW);
    float Vis = V_SmithGGXCorrelated(NoV, NoL, alpha);
    vec3  F  = F_Schlick(F0, VoH);

    vec3 spec = D * Vis * F * comp;
    vec3 diff = diffC * Fd_Burley(NoV, NoL, LoH, rough) * (vec3(1.0) - F);
    return (diff + spec) * NoL;
}

// ---------------------------------------------------------------- ambient / env

vec3 sampleSH(vec3 n)
{
    vec4 n4 = vec4(n, 1.0);
    vec3 x1 = vec3(dot(uSHAr, n4), dot(uSHAg, n4), dot(uSHAb, n4));
    vec4 vB = n.xyzz * n.yzzx;
    vec3 x2 = vec3(dot(uSHBr, vB), dot(uSHBg, vB), dot(uSHBb, vB));
    float vC = n.x * n.x - n.y * n.y;
    vec3 x3 = uSHC.rgb * vC;
    return max(x1 + x2 + x3, vec3(0.0));
}

vec3 ambientDiffuse(vec3 n)
{
    if (uAmbientMode == 0) return uAmbientColor * uAmbientIntensity;
    if (uAmbientMode == 1)
    {
        float t = n.y;
        vec3 c = t >= 0.0 ? mix(uAmbientMiddle, uAmbientTop,    smoothstep(0.0, 1.0,  t))
                          : mix(uAmbientMiddle, uAmbientBottom, smoothstep(0.0, 1.0, -t));
        return c * uAmbientIntensity;
    }
    return sampleSH(n) * uAmbientIntensity * uEnvIntensity;
}

vec2 dirToEquirect(vec3 d)
{
    return vec2(atan(d.z, d.x) / (2.0 * PI) + 0.5,
                asin(clamp(d.y, -1.0, 1.0)) / PI + 0.5);
}

// ---------------------------------------------------------------- shadows

float sunShadow(vec3 N, vec3 L)
{
    float NoL = saturate(dot(N, L));
    float sinT = sqrt(1.0 - NoL * NoL);
    vec3 p = vFragPos + N * (uShadowNormalOffset * uShadowTexelWorld * mix(0.3, 1.0, sinT));

    vec4 ls = uLightSpace * vec4(p, 1.0);
    vec3 c = ls.xyz / ls.w * 0.5 + 0.5;
    if (c.z > 1.0 || any(lessThan(c.xy, vec2(0.0))) || any(greaterThan(c.xy, vec2(1.0))))
        return 1.0;

    float depth = c.z - uShadowBias;
    vec2 texel = 1.0 / vec2(textureSize(uShadowMap, 0));
    float phi = 6.2831853 * ign(gl_FragCoord.xy);

    const int TAPS = 16;
    float sum = 0.0;
    for (int k = 0; k < TAPS; ++k)
    {
        vec2 o = vogelDisk(k, TAPS, phi) * SUN_SOFTNESS;
        sum += texture(uShadowMap, vec3(c.xy + o * texel, depth));
    }
    return sum / float(TAPS);
}

float pointShadow(float slot, vec3 lightToFrag, float dist, float range, float NoL)
{
    float cur = dist / range - POINT_BIAS * (1.0 + 2.0 * (1.0 - NoL));
    vec3 dir = lightToFrag / dist;

    vec3 up = abs(dir.y) < 0.99 ? vec3(0.0, 1.0, 0.0) : vec3(1.0, 0.0, 0.0);
    vec3 t = normalize(cross(up, dir));
    vec3 b = cross(dir, t);

    float jitter = POINT_SOFTNESS * dist;
    float phi = 6.2831853 * ign(gl_FragCoord.xy + 17.0);

    const int TAPS = 8;
    float lit = 0.0;
    for (int k = 0; k < TAPS; ++k)
    {
        vec2 o = vogelDisk(k, TAPS, phi) * jitter;
        float stored = texture(uPointShadowMap, vec4(dir + t * o.x + b * o.y, slot)).r;
        lit += (cur > stored) ? 0.0 : 1.0;
    }
    return lit / float(TAPS);
}

// ============================================================================
//  Main
// ============================================================================

void main()
{
    // ---- surface
    vec4 tex = (uHasTexture != 0) ? texture(uTexture, vUV) : vec4(1.0);
    vec3 albedo = uColor * tex.rgb;
    float outAlpha = uAlpha * tex.a;

    vec3 N = normalize(vNormal);
    if (!gl_FrontFacing) N = -N;
    vec3 V = normalize(uViewPos - vFragPos);
    float NoV = max(dot(N, V), 1e-4);

    float metallic = saturate(uMetallic);
    float rough = clamp(1.0 - uSmoothness, 0.03, 1.0);
    float alpha = rough * rough;

    // specular anti-aliasing (Tokuyoshi/Kaplanyan): widen lobe where the normal varies fast
    {
        vec3 dndx = dFdx(N);
        vec3 dndy = dFdy(N);
        float variance = 0.25 * (dot(dndx, dndx) + dot(dndy, dndy));
        alpha = sqrt(saturate(alpha * alpha + min(2.0 * variance, 0.18)));
    }

    vec3 F0 = mix(vec3(0.04), albedo, metallic);
    vec3 diffC = albedo * (1.0 - metallic);

    // multi-scatter energy compensation (Fdez-Aguera)
    vec2 ab = envBRDF(rough, NoV);
    vec3 comp = vec3(1.0) + F0 * (1.0 / max(ab.x + ab.y, 1e-3) - 1.0);

    vec3 Lo = vec3(0.0);

    // ---- sun lights (uShadowMap / uLightSpace belong to the first light with uSunShadow > 0)
    int sunCount = min(uSunLightCount, MAX_SUN_LIGHTS);
    int shadowSun = -1;
    for (int i = 0; i < sunCount; ++i)
        if (uSunShadow[i] > 0.0) { shadowSun = i; break; }

    for (int i = 0; i < sunCount; ++i)
    {
        vec3 L = normalize(-uSunLightDir[i]);
        vec3 radiance = uSunLightColor[i] * uSunLightIntensity[i];
        vec3 contrib = shadeLight(N, V, L, NoV, alpha, uSunAngularRadius, rough, F0, diffC, comp);

        if (i == shadowSun && dot(contrib, vec3(1.0)) > 0.0)
            contrib *= mix(1.0, sunShadow(N, L), saturate(uSunShadow[i]));

        Lo += contrib * radiance;
    }

    // ---- point lights
    int pointCount = min(uPointLightCount, MAX_POINT_LIGHTS);
    for (int i = 0; i < pointCount; ++i)
    {
        vec3 toLight = uPointLightPos[i] - vFragPos;
        float d = length(toLight);
        float range = uPointLightRange[i];
        if (d >= range || d < 1e-4) continue;

        vec3 L = toLight / d;
        float NoL = saturate(dot(N, L));
        if (NoL <= 0.0) continue;

        float win = saturate(1.0 - sq(sq(d / range)));
        float att = win * win / (d * d + 1.0);

        float widen = uPointLightSourceRadius / (3.0 * d);
        vec3 contrib = shadeLight(N, V, L, NoV, alpha, widen, rough, F0, diffC, comp);

        float slot = uPointShadowSlot[i];
        if (slot >= 0.0)
            contrib *= pointShadow(slot, -toLight, d, range, NoL);

        Lo += contrib * uPointLightColor[i] * uPointLightIntensity[i] * att;
    }

    // ---- image based ambient
    vec3 kdEnv = (vec3(1.0) - (F0 * ab.x + ab.y)) ; // energy left for diffuse
    vec3 ambDiff = ambientDiffuse(N) * diffC * kdEnv;

    vec3 R = reflect(-V, N);
    R = normalize(mix(R, N, rough * rough));          // bend rough reflections toward normal
    float lod = rough * uMaxReflectionLod;
    vec3 envSpec = textureLod(uSkybox, dirToEquirect(R), lod).rgb;

    // horizon specular occlusion
    float horizon = saturate(1.0 + dot(R, N));
    horizon *= horizon;

    vec3 ambSpec = envSpec * (F0 * ab.x + ab.y) * comp
                 * horizon * uReflectionIntensity * uEnvIntensity;

    vec3 color = Lo + ambDiff + ambSpec;

    // tiny dither against banding in dark gradients
    color += (ign(gl_FragCoord.xy + 3.0) - 0.5) / 255.0;

    FragColor = vec4(max(color, vec3(0.0)), outAlpha);
    FragNormal = normalize(vViewNormal);
}