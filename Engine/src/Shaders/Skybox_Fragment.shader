#version 330 core

in vec2 vScreenPos;

const float PI = 3.14159265359;

uniform mat4 uView;
uniform mat4 uProjection;
uniform sampler2D uTexture; /// baked sky, sRGB8 (GL decodes to linear on sampling)
uniform float uBlurScale = 0.0;

out vec4 FragColor;


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

    /// Already exposed and tonemapped at bake time (display-referred, 0..1).
    /// The resolve pass only applies gamma to sky pixels.
    vec3 color = textureLod(uTexture, uv, uBlurScale).rgb;

    FragColor = vec4(color, 1.0);
}