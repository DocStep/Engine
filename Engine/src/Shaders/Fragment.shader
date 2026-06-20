#version 330 core

in vec3 vNormal;
in vec3 vFragPos;

out vec4 FragColor;

uniform vec3  uColor;
uniform float uRoughness;
uniform float uMetallic;
uniform float uAmbient;

uniform vec3 uSunLightColor;
uniform float uSunLightIntensity;
uniform vec3 uSunLightDir;
uniform vec3 uViewPos;

void main () {
    vec3 normal = normalize(vNormal);
    vec3 lightDir = normalize(-uSunLightDir);
    vec3 viewDir = normalize(uViewPos - vFragPos);
    vec3 halfDir = normalize(lightDir + viewDir);

    // Ambient
    vec3 ambient = uAmbient * uColor;

    // Diffuse — metallic surfaces reflect less diffuse
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = diff * uSunLightColor * uSunLightIntensity * uColor * (1.0 - uMetallic * 0.8);

    // Specular — Blinn-Phong, driven by roughness
    // roughness 0 = mirror (shininess 256), roughness 1 = matte (shininess 2)
    float shininess = mix(256.0, 2.0, uRoughness);
    float spec = pow(max(dot(normal, halfDir), 0.0), shininess);
    // Metallic surfaces tint specular by albedo color
    vec3 specColor = mix(vec3(1.0), uColor, uMetallic);
    float specStr = mix(0.04, 1.0, uMetallic) + (1.0 - uRoughness) * 0.96;
    vec3 specular = spec * uSunLightColor * uSunLightIntensity * specColor * specStr;

    vec3 result = ambient + diffuse + specular;

    // Tone-map to avoid blowout on bright specular
    result = result / (result + vec3(1.0));

    FragColor = vec4(result, 1.0);
}
