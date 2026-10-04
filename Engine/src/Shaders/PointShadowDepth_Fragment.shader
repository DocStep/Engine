#version 330 core

in vec3 vWorldPos;

uniform vec3 uLightPos;
uniform float uLightRange;

void main () {
    gl_FragDepth = length(vWorldPos - uLightPos)/uLightRange; /// linear 0..1
}
