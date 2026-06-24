#version 330 core
in vec2 vUV;

uniform sampler2D uTexture;

out vec4 FragColor;

void main() {
    float alpha = texture(uTexture, vUV).a;
    FragColor = vec4(1.0, 1.0, 1.0, alpha);
}
