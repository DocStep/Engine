#version 330 core
in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uScene;
uniform sampler2D uDepth;

uniform float uNear;
uniform float uFar;
uniform float uFocusDistance;
uniform float uFocusRange;
uniform float uBokehRadius;
uniform vec2 uTexelSize;

float LinearizeDepth (float depth) {
    float z = depth*2.0 - 1.0;
    return (2.0*uNear*uFar) / (uFar + uNear - z*(uFar - uNear));
}

void main () {
    float depth = texture(uDepth, vUV).r;
    float linearDepth = LinearizeDepth(depth);

    float coc = clamp(abs(linearDepth - uFocusDistance) / uFocusRange, 0.0, 1.0);
    float radius = coc*uBokehRadius;

    vec3 sum = texture(uScene, vUV).rgb;
    float total = 1.0;

    const int kSamples = 8;
    for (int i = 0; i < kSamples; i++) {
        float angle = float(i) / float(kSamples)*6.28318530718;
        vec2 offset = vec2(cos(angle), sin(angle))*radius*uTexelSize;
        sum += texture(uScene, vUV + offset).rgb;
        total += 1.0;
    }

    FragColor = vec4(sum / total, 1.0);
}