#version 330 core

in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uEnvMap;
uniform float uRoughness;
uniform float uResolutionX;
uniform float uResolutionY;
uniform int uSampleCount;
// uniform float uFireflyClamp;

const float PI = 3.14159265359;

/// inverse of SampleSphericalMap: UV -> direction (y up)
vec3 DirFromUV(vec2 uv) {
    float phi = (uv.x - 0.5)*2.0*PI;
    float lat = (uv.y - 0.5)*PI;
    return vec3(cos(lat)*cos(phi), sin(lat), cos(lat)*sin(phi));
}

vec2 SampleSphericalMap(vec3 v) {
    return vec2(atan(v.z, v.x), asin(clamp(v.y, -1.0, 1.0)))*vec2(0.1591, 0.3183) + 0.5;
}

float RadicalInverse(uint bits) {
    bits = (bits << 16u) | (bits >> 16u);
    bits = ((bits & 0x55555555u) << 1u) | ((bits & 0xAAAAAAAAu) >> 1u);
    bits = ((bits & 0x33333333u) << 2u) | ((bits & 0xCCCCCCCCu) >> 2u);
    bits = ((bits & 0x0F0F0F0Fu) << 4u) | ((bits & 0xF0F0F0F0u) >> 4u);
    bits = ((bits & 0x00FF00FFu) << 8u) | ((bits & 0xFF00FF00u) >> 8u);
    return float(bits)*2.3283064365386963e-10;
}

vec2 Hammersley(uint i, uint n) {
    return vec2(float(i)/float(n), RadicalInverse(i));
}

/// GGX half vector in tangent space
vec3 ImportanceSampleGGX(vec2 Xi, float a) {
    float phi = 2.0*PI*Xi.x;
    float cosT = sqrt((1.0 - Xi.y)/(1.0 + (a*a - 1.0)*Xi.y));
    float sinT = sqrt(max(1.0 - cosT*cosT, 0.0));
    return vec3(sinT*cos(phi), sinT*sin(phi), cosT);
}

float DistributionGGX(float NdotH, float a) {
    float a2 = a*a;
    float d = NdotH*NdotH*(a2 - 1.0) + 1.0;
    return a2/(PI*d*d);
}

void main() {
    /// level 0 (roughness 0) = mirror, plain copy of the source.
    /// stays under the half-float max (65504).
    if (uRoughness < 0.001) {
        vec3 c = textureLod(uEnvMap, vUV, 0.0).rgb;
        FragColor = vec4(min(c, vec3(60000.0)), 1.0);
        return;
    }

    vec3 N = DirFromUV(vUV);
    vec3 V = N;

    vec3 up = abs(N.y) < 0.999 ? vec3(0.0, 1.0, 0.0) : vec3(1.0, 0.0, 0.0);
    vec3 T = normalize(cross(up, N));
    vec3 B = cross(N, T);

    float a = uRoughness*uRoughness;
    float saTexel = 4.0*PI/(uResolutionX*uResolutionY); /// average solid angle of a source texel
    uint count = uint(max(uSampleCount, 1));

    vec3 sum = vec3(0.0);
    float weight = 0.0;

    for (uint i = 0u; i < count; i++) {
        vec2 Xi = Hammersley(i, count);
        vec3 Ht = ImportanceSampleGGX(Xi, a);
        vec3 H = normalize(T*Ht.x + B*Ht.y + N*Ht.z);
        vec3 L = normalize(2.0*dot(V, H)*H - V);

        float NdotL = dot(N, L);
        if (NdotL < 0.0) continue;

        float NdotH = max(dot(N, H), 0.0);
        float VdotH = max(dot(V, H), 1e-4);

        /// filtered importance sampling: bright small features (the sun) are
        /// read from a blurrier source mip instead of becoming rare hot samples.
        float pdf = DistributionGGX(NdotH, a)*NdotH/(4.0*VdotH);
        float saSample = 1.0/(float(count)*pdf + 1e-4);
        float mip = max(0.5*log2(saSample/saTexel) + 1.0, 0.0);

        vec3 c = textureLod(uEnvMap, SampleSphericalMap(L), mip).rgb;
        // c = min(c, vec3(uFireflyClamp));

        sum += c*NdotL;
        weight += NdotL;
    }

    vec3 result = weight < 0.0001 ? textureLod(uEnvMap, vUV, 0.0).rgb : sum/weight;
    FragColor = vec4(min(result, vec3(60000.0)), 1.0);
}