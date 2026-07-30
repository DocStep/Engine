#version 330 core
in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uScene; /// blurred AO, arrives here via the normal chain
uniform sampler2D uOriginal; /// original lit scene, bound manually

void main () {
    float ao = texture(uScene, vUV).r;
    vec3 sceneColor = texture(uOriginal, vUV).rgb;
    FragColor = vec4(sceneColor*ao, 1.0);
}
