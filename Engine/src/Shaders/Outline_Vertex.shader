#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;
uniform float uOutlineWidth;

void main () {
    vec3 dir = normalize(aPosition); /// assumes mesh is centered at local origin
    vec3 scaledPos = aPosition + dir*uOutlineWidth;
    gl_Position = uProjection*uView*uModel*vec4(scaledPos, 1.0);
}