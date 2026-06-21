#version 330 core
in vec3 vWorldPos;

uniform vec3 uColor;
uniform vec3 uCameraPos;
uniform float uRadius;
uniform float uFade;
uniform float uAlpha;

out vec4 FragColor;

void main () {
    float dist = length(vWorldPos - uCameraPos);
    float alpha = 1.0 - smoothstep(uRadius - uFade, uRadius, dist);
    FragColor = vec4(uColor, alpha*uAlpha);
}