#version 330 core

// GGX importance-sampled convolution of an equirect env map, one call per dest mip.
// N=V=R split-sum, filtered importance sampling (GPU Gems 3 ch.20) with per-pixel rotation.

in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uEnvMap;   // ORIGINAL sharp equirect with full mip chain
uniform float uRoughness;
uniform float uResolutionX;
uniform float uResolutionY;
uniform float uFireflyClamp; // set very high (1000+) or disable
uniform int uSampleCount;  // e.g. 128 low roughness, 512-1024 high

const float PI = 3.14159265359;
const float MIP_BIAS = 1.0;
const float MIN_COS_LAT = 0.1;

// Must be the exact inverse of DirToUV/SampleSphericalMap in your Lit shader.
vec3 UVToDir (vec2 uv) {
    float phi = (uv.x - 0.5)*2.0*PI;
    float theta = (uv.y - 0.5)*PI;
    float cosTheta = cos(theta);
    return normalize(vec3(cosTheta*cos(phi), sin(theta), cosTheta*sin(phi)));
}
vec2 DirToUV (vec3 v) {
    float u = 0.5 + atan(v.z, v.x)/(2.0*PI);
    float vv = 0.5 + asin(clamp(v.y, -1.0, 1.0))/PI;
    return vec2(u, vv);
}

float RadicalInverse_VdC (uint bits) {
    bits = (bits << 16u) | (bits >> 16u);
    bits = ((bits & 0x55555555u) << 1u) | ((bits & 0xAAAAAAAAu) >> 1u);
    bits = ((bits & 0x33333333u) << 2u) | ((bits & 0xCCCCCCCCu) >> 2u);
    bits = ((bits & 0x0F0F0F0Fu) << 4u) | ((bits & 0xF0F0F0F0u) >> 4u);
    bits = ((bits & 0x00FF00FFu) << 8u) | ((bits & 0xFF00FF00u) >> 8u);
    return float(bits)*2.3283064365386963e-10;
}
vec2 Hammersley (uint i, uint N) {
    return vec2(float(i)/float(N), RadicalInverse_VdC(i));
}

// Interleaved gradient noise (Jimenez)
float IGN (vec2 p) {
    return fract(52.9829189*fract(dot(p, vec2(0.06711056, 0.00583715))));
}

vec3 ImportanceSampleGGX (vec2 Xi, vec3 N, float a) {
    float phi = 2.0*PI*Xi.x;
    float cosTheta = sqrt((1.0 - Xi.y)/(1.0 + (a*a - 1.0)*Xi.y));
    float sinTheta = sqrt(max(1.0 - cosTheta*cosTheta, 0.0));

    vec3 H = vec3(cos(phi)*sinTheta, sin(phi)*sinTheta, cosTheta);

    vec3 up = abs(N.y) < 0.999 ? vec3(0.0, 1.0, 0.0) : vec3(1.0, 0.0, 0.0);
    vec3 tangent = normalize(cross(up, N));
    vec3 bitangent = cross(N, tangent);

    return normalize(tangent*H.x + bitangent*H.y + N*H.z);
}

float DistributionGGX_D (float NdotH, float a2) {
    float d = NdotH*NdotH*(a2 - 1.0) + 1.0;
    return a2/(PI*d*d + 1e-7);
}

void main () {
    // Mirror-sharp level: straight copy
    if (uRoughness <= 0.0) {
        FragColor = vec4(textureLod(uEnvMap, vUV, 0.0).rgb, 1.0);
        return;
    }

    vec3 N = UVToDir(vUV);
    vec3 V = N;

    float a = uRoughness*uRoughness;
    float a2 = a*a;

    float rot = IGN(gl_FragCoord.xy);
    float invN = 1.0/float(uSampleCount);

    vec3 prefiltered = vec3(0.0);
    float totalWeight = 0.0;

    for (int i = 0; i < uSampleCount; i++) {
        vec2 Xi = Hammersley(uint(i), uint(uSampleCount));
        Xi.x = fract(Xi.x + rot);

        vec3 H = ImportanceSampleGGX(Xi, N, a);
        vec3 L = normalize(2.0*dot(V, H)*H - V);

        float NdotL = max(dot(N, L), 0.0);
        if (NdotL > 0.0) {
            float NdotH = max(dot(N, H), 0.0);
            float HdotV = max(dot(H, V), 0.0);
            float D = DistributionGGX_D(NdotH, a2);
            float pdf = D*NdotH/(4.0*HdotV + 1e-5) + 1e-5;

            // Equirect texel solid angle, capped near poles (mips are isotropic)
            float cosLat = max(sqrt(max(1.0 - L.y*L.y, 0.0)), MIN_COS_LAT);
            float saTexel = (2.0*PI/uResolutionX)*(PI/uResolutionY)*cosLat;

            float saSample = invN/pdf;
            float mipLevel = max(0.5*log2(saSample/saTexel) + MIP_BIAS, 0.0);

            vec3 c = textureLod(uEnvMap, DirToUV(L), mipLevel).rgb;

            float lum = dot(c, vec3(0.2126, 0.7152, 0.0722));
            if (lum > uFireflyClamp) c *= uFireflyClamp/lum;

            prefiltered += c*NdotL;
            totalWeight += NdotL;
        }
    }

    FragColor = vec4(totalWeight > 0.0 ? prefiltered/totalWeight : vec3(0.0), 1.0);
}