#version 330 core
uniform vec3 uColor;
uniform float uAlpha;

uniform vec3 uCameraPos;
uniform float uRadius;
uniform float uFade;

out vec4 FragColor;

void main () {
    FragColor = vec4(uColor, uAlpha);
}
