#version 330 core

in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uAO;

void main () {
    FragColor = vec4(vec3(texture(uAO, vUV).r), 1.0);
}
