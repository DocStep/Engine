#version 330 core
#extension GL_ARB_separate_shader_objects : enable

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
// flat out int vInstanceId;


void main () {
    vec4 worldPos = aModel*vec4(aPosition, 1.0);
    
    // vInstanceId = gl_InstanceID;
    vFragPos = worldPos.xyz;
    vNormal = normalize(transpose(inverse(mat3(aModel)))*aNormal);
    vUV = aUV;
    vViewNormal = mat3(uView)*vNormal;

    gl_Position = uProjection*uView*worldPos;
}
