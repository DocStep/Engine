#version 330 core

out vec2 vUV;

void main () {
    vUV = vec2((gl_VertexID << 1) & 2, gl_VertexID & 2);
    gl_Position = vec4(vUV*2f - 1f, 0f, 1f);
}
