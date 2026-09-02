#version 330 core

in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uInputTexture;
uniform float uVignetteIntensity; /// 0 = none, 1 = strong
uniform float uVignetteRadius; /// distance where falloff starts, ~0.75 typical
uniform float uVignetteSoftness; /// falloff width, ~0.45 typical
uniform vec3 uVignetteColor; /// usually black (0,0,0)

void main() {
    vec3 color = texture(uInputTexture, vUV).rgb;

    vec2 centered = vUV - vec2(0.5);
    centered.x *= 1.0;///adjust here if you want aspect-corrected ellipse instead of screen-space circle
    float dist = length(centered);

    float vignette = 1.0 - smoothstep(uVignetteRadius, uVignetteRadius + uVignetteSoftness, dist);
    vignette = mix(1.0, vignette, uVignetteIntensity);

    color = mix(uVignetteColor, color, vignette);

    FragColor = vec4(color, 1.0);
}
