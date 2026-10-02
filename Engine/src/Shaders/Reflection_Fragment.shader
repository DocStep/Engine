#version 330 core

/// Reflect_Fragment.shader: unlit, full mirror reflection of the skybox

in vec3 vNormal;
in vec3 vFragPos;
in vec2 vUV;
in vec3 vViewNormal;

uniform vec3 uViewPos;
uniform sampler2D uSkybox;

layout (location = 0) out vec4 FragColor;
layout (location = 1) out vec4 FragNormal;

vec2 SampleSphericalMap(vec3 v) {
    return vec2(atan(v.z, v.x), asin(v.y))*vec2(0.1591, 0.3183) + 0.5;
}

void main() {
    vec3 N = normalize(vNormal);
    vec3 V = normalize(uViewPos - vFragPos);
    vec3 R = reflect(-V, N);

    vec3 color = textureLod(uSkybox, SampleSphericalMap(R), 0.0).rgb;
    color = pow(color, vec3(1.0/2.2));

    FragColor = vec4(color, 1.0);
    FragNormal = vec4(vViewNormal, 1.0);
}