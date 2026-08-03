#version 330 core
in vec3 vWorldPos;

uniform vec3 uCameraPos;
uniform vec3 uColor;
uniform float uRadius;
uniform float uFade;
uniform float uAlpha;

out vec4 FragColor;

void main () {
    vec2 dirDist = vec2(vWorldPos.x - uCameraPos.x, vWorldPos.z - uCameraPos.z);
    float alpha = 1.0 - smoothstep(uFade, uRadius, length(dirDist));

    vec3 dir = vWorldPos - uCameraPos;
    float angle = dot(normalize(abs(1 - dir)), vec3(0, 1, 0));
    angle = angle*angle;

    FragColor = vec4(uColor, angle*alpha*uAlpha);
}
