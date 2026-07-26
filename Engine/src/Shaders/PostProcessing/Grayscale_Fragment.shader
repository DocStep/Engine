#version 330 core

in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uScene;

void main () {
    vec3 color = texture(uScene, vUV).rgb;
    float gray = dot(color, vec3(0.299f, 0.587f, 0.114f));
    FragColor = vec4(gray, gray, gray, 1f);
}
