#version 330 core
in vec3 vWorldPos;

uniform vec3 uCameraPos;
uniform float uRadius;
uniform float uFade;
uniform float uAlpha;

out vec4 FragColor;

void main () {
    float alpha = 1;

    vec3 color = vWorldPos;
    if (vWorldPos.x != 0) {
        alpha = 1.0 - smoothstep(uFade, uRadius, abs(vWorldPos.x - uCameraPos.x));
        if (0 < vWorldPos.x) color.x = 1;
        else {
            color.x = 1;
            alpha = 0.2*alpha;
        }
    } else if (vWorldPos.y != 0) {
        alpha = 1.0 - smoothstep(uFade, uRadius, abs(vWorldPos.y - uCameraPos.y));
        if (0 < vWorldPos.y) color.y = 1;
        else {
            color.y = 1;
            alpha = 0.2*alpha;
        }
    } else if (vWorldPos.z != 0) {
        alpha = 1.0 - smoothstep(uFade, uRadius, abs(vWorldPos.z - uCameraPos.z));
        if (0 < vWorldPos.z) color.z = 1;
        else {
            color.z = 1;
            alpha = 0.2*alpha;
        }
    }

    FragColor = vec4(color, alpha*uAlpha);
}