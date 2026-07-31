#version 330 core

in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uSceneColor;

void main () {
    vec3 color = texture(uSceneColor, vUV).rgb;
    float gray = dot(color, vec3(0.299, 0.587, 0.114));
    FragColor = vec4(gray, gray, gray, 1.0);
}
