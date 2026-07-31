#version 330 core
in vec2 vUV;
out vec4 FragColor;
uniform sampler2D uSceneColor;

void main () {
    FragColor = vec4(texture(uSceneColor, vUV).rgb, 1.0);
}