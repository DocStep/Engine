#version 330 core

out vec2 vScreenPos;

void main () {
    vec2 positions[3] = vec2[3](
        vec2(-1.0, -1.0),
        vec2( 3.0, -1.0),
        vec2(-1.0,  3.0)
    );

    vScreenPos = positions[gl_VertexID];
    gl_Position = vec4(vScreenPos, 0.0, 1.0);
}
