#version 330 core

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

void main() {
    gl_FragDepth = 1.0;
}
