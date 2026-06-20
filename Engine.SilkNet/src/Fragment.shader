#version 330 core
in vec3 vNormal;
in vec3 vWorldPos;

uniform vec3 uColor;
uniform vec3 uLightDir;   // direction the light travels (normalized)
uniform vec3 uLightColor;
uniform vec3 uViewPos;

out vec4 FragColor;

void main () {
    vec3 normal = normalize(vNormal);
    vec3 lightDir = normalize(-uLightDir);

    float ambientStrength = 0.35;
    vec3 ambient = ambientStrength * uLightColor;

    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = diff * uLightColor;

    vec3 result = (ambient + diffuse) * uColor;
    FragColor = vec4(result, 1.0);
}
