#version 330 core

in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uScene; /// this is the AO buffer here, not the lit scene
uniform vec2 uTexelSize;

void main () {
    vec3 sum = vec3(0.0);
    for (int x = -2; x <= 2; x++)
        for (int y = -2; y <= 2; y++)
            sum += texture(uScene, vUV + vec2(x, y)*uTexelSize).rgb;
    FragColor = vec4(sum/25.0, 1.0);
}