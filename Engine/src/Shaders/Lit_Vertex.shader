#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aUV;
layout (location = 3) in mat4 aModel;
layout (location = 7) in mat4 aNormalMatrix;

uniform mat4 uView;
uniform mat4 uProjection;

out vec3 vNormal;
out vec3 vFragPos;
out vec2 vUV;
out vec3 vViewNormal;


void main () {
    vec4 worldPos = aModel*vec4(aPosition, 1.0);

    vFragPos = worldPos.xyz;
    vNormal = mat3(aNormalMatrix)*aNormal;
    vUV = aUV;
    vViewNormal = mat3(uView) * vNormal;
    // vUV = vec2(aUV.x, 1.0 - aUV.y);

    gl_Position = uProjection*uView*worldPos;
}
