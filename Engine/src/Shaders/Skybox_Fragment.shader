#version 330 core

in vec2 vScreenPos;
out vec4 FragColor;

uniform mat4 uView;
uniform mat4 uProjection;
uniform sampler2D uTexture;
uniform float uBlurScale;

const float PI = 3.14159265359;

/// Converts a world-space direction into equirectangular UV coords.
vec2 DirectionToEquirectUV (vec3 dir) {
    float u = 0.5 + atan(dir.z, dir.x)/(2.0*PI);
    float v = 0.5 + asin(clamp(dir.y, -1.0, 1.0))/PI;
    return vec2(u, v);
}

void main () {
    vec4 clipPos = vec4(vScreenPos, 1.0, 1.0);
    vec4 viewPos = uProjection*clipPos;
    viewPos /= viewPos.w;

    vec3 worldDir = normalize((uView*vec4(viewPos.xyz, 0.0)).xyz);
    vec2 uv = DirectionToEquirectUV(worldDir);
    vec3 color = textureLod(uTexture, uv, uBlurScale).rgb;

    /// Linear HDR out. Exposure, tonemap and gamma happen in the resolve pass.
    FragColor = vec4(color, 1.0);
}