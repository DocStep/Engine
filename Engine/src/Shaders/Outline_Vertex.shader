#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;

uniform mat4 uView;
uniform mat4 uProjection;
uniform mat4 uModel;
uniform float uNormalOffset;

void main () {
    vec3 normal = normalize(aNormal);

    vec3 pos = aPosition + normal*uNormalOffset;

    gl_Position = uProjection*uView*uModel*vec4(pos, 1.0);
}
