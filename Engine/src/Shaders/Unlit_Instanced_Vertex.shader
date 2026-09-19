#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 3) in mat4 aModel;

uniform mat4 uView;
uniform mat4 uProjection;

out vec3 vWorldPos;

void main () {
    vWorldPos = (aModel * vec4(aPosition, 1.0)).xyz;
    gl_Position = uProjection * uView * aModel * vec4(aPosition, 1.0);
}
