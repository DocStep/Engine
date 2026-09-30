#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec3 vViewNormal;

void main () {
    mat4 modelView = uView*uModel;
    /// inverse-transpose so non-uniform scale doesn't skew the normal
    vViewNormal = transpose(inverse(mat3(modelView)))*aNormal;
    gl_Position = uProjection*modelView*vec4(aPosition, 1.0);
}