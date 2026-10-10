#version 330 core

in vec2 vScreenPos;

const float PI = 3.14159265359;
const float knee = 1.0;

uniform mat4 uView;
uniform mat4 uProjection;
uniform sampler2D uTexture;
uniform float uExposure = 0.5; /// tune per HDR
uniform float uBlurScale = 0.0;

out vec4 FragColor;


/// Converts a world-space direction into equirectangular UV coords.
vec2 DirectionToEquirectUV (vec3 dir) {
    float u = 0.5 + atan(dir.z, dir.x)/(2.0*PI);
    float v = 0.5 + asin(clamp(dir.y, -1.0, 1.0))/PI;
    return vec2(u, v);
}

vec3 PBRNeutral (vec3 color) {
    const float startCompression = 0.8 - 0.04;
    const float desaturation = 0.15;
    float x = min(color.r, min(color.g, color.b));
    float offset = x < 0.08 ? x - 6.25*x*x : 0.04;
    color -= offset;
    float peak = max(color.r, max(color.g, color.b));
    if (peak < startCompression) return color;
    const float d = 1.0 - startCompression;
    float newPeak = 1.0 - d*d/(peak + d - startCompression);
    color *= newPeak/peak;
    float g = 1.0 - 1.0/(desaturation*(peak - newPeak) + 1.0);
    return mix(color, newPeak*vec3(1.0), g);
}


void main () {
    vec4 clipPos = vec4(vScreenPos, 1.0, 1.0);
    vec4 viewPos = uProjection*clipPos;
    viewPos /= viewPos.w;

    vec3 worldDir = normalize((uView*vec4(viewPos.xyz, 0.0)).xyz);
    vec2 uv = DirectionToEquirectUV(worldDir);
    vec3 color = textureLod(uTexture, uv, uBlurScale).rgb;

    color *= uExposure;

    /// Linear HDR out. Exposure, tonemap and gamma happen in the resolve pass.
    FragColor = vec4(color, 1.0);
}