#version 330 core
in vec3 vWorldPos;

uniform vec3 uColor;
uniform vec3 uCameraPos;
uniform float uRadius;
uniform float uFade;
uniform float uAlpha;

out vec4 FragColor;

void main () {
    vec3 dir = vWorldPos - uCameraPos;
    float dist = length(dir);
    float alpha = 1.0 - smoothstep(uFade, uRadius, dist);
    float angle = dot(normalize(abs(dir)), vec3(0, 1, 0));
    angle = 1f-angle;
    angle = 1f-angle;
    FragColor = vec4(uColor, angle*alpha*uAlpha);
}