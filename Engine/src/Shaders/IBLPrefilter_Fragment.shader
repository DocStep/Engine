#version 330 core

// GGX importance-sampled convolution of an equirectangular env map, evaluated
// at a fixed roughness (one call per destination mip level). N=V=R split-sum
// assumption — standard Karis 2013 approach, adapted to equirect instead of cubemap.

in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uEnvMap; /// source — the ORIGINAL sharp equirect, never a previous mip
uniform float uRoughness;
uniform float uResolutionX;
uniform float uResolutionY;
uniform float uFireflyClamp;

const float PI = 3.14159265359;
const uint SAMPLE_COUNT = 256u; /// one-time bake cost, not per-frame — safe to push higher for quality

/// Must be the exact inverse of whatever DirToUV/SampleSphericalMap your Lit_Fragment uses,
/// or prefiltered samples land at the wrong spot on the source image.
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

vec3 ImportanceSampleGGX (vec2 Xi, vec3 N, float roughness) {
    float a = roughness*roughness;

    float phi = 2.0*PI*Xi.x;
    float cosTheta = sqrt((1.0 - Xi.y)/(1.0 + (a*a - 1.0)*Xi.y));
    float sinTheta = sqrt(1.0 - cosTheta*cosTheta);

    vec3 H = vec3(cos(phi)*sinTheta, sin(phi)*sinTheta, cosTheta);

    /// Smooth, continuous choice of reference axis instead of a hard N.z threshold —
    /// avoids a visible seam/discontinuity in the tangent basis across the sphere.
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
    vec3 N = UVToDir(vUV);
    vec3 V = N;

    vec3 prefiltered = vec3(0.0);
    float totalWeight = 0.0;

    float a2 = uRoughness*uRoughness*uRoughness*uRoughness;

    for (uint i = 0u; i < SAMPLE_COUNT; i++) {
        vec2 Xi = Hammersley(i, SAMPLE_COUNT);
        vec3 H = ImportanceSampleGGX(Xi, N, uRoughness);
        vec3 L = normalize(2.0*dot(V, H)*H - V);

        float NdotL = max(dot(N, L), 0.0);
        if (NdotL > 0.0) {
            float NdotH = max(dot(N, H), 0.0);
            float HdotV = max(dot(H, V), 0.0);
            float D = DistributionGGX_D(NdotH, a2);
            float pdf = D*NdotH/(4.0*HdotV) + 1e-5;

            /// Equirect texel solid angle shrinks toward the poles (cos(latitude)) — L.y is
            /// this sample's latitude (matches UVToDir's sin(theta) = v.y convention).
            /// Without this, pole-region samples get an under-blurred mip and show raw
            /// projection distortion as warping, worse at high roughness where more samples
            /// wander toward the poles.
            float cosLat = max(abs(cos(asin(clamp(L.y, -1.0, 1.0)))), 1e-3);
            float saTexel = (2.0*PI/uResolutionX)*(PI/uResolutionY)*cosLat;

            float saSample = 1.0/(float(SAMPLE_COUNT)*pdf + 1e-5);
            float mipLevel = uRoughness == 0.0 ? 0.0 : 0.5*log2(saSample/saTexel);

            vec3 sampleColor = textureLod(uEnvMap, DirToUV(L), mipLevel).rgb;

            float luminance = dot(sampleColor, vec3(0.2126, 0.7152, 0.0722));
            if (luminance > uFireflyClamp) sampleColor *= uFireflyClamp/luminance;

            prefiltered += sampleColor*NdotL;
            totalWeight += NdotL;
        }
    }

    FragColor = vec4(totalWeight > 0.0 ? prefiltered/totalWeight : vec3(0.0), 1.0);
}
