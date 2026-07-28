#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;
uniform float uNormalOffset;

out vec3 vWorldPos;

void main () {
    vec3 worldPos = (uModel * vec4(aPosition, 1.0)).xyz;
    mat3 normalMatrix = transpose(inverse(mat3(uModel)));
    vec3 worldNormal = normalize(normalMatrix * aNormal);

    worldPos += worldNormal*uNormalOffset;

    vWorldPos = worldPos;
    gl_Position = uProjection * uView * vec4(worldPos, 1.0);
}
