#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 3) in mat4 aModel;

uniform mat4 uFaceViewProj;

out vec3 vWorldPos;

void main () {
    vec4 world = aModel*vec4(aPosition, 1.0);
    vWorldPos = world.xyz;
    gl_Position = uFaceViewProj*world;
}
