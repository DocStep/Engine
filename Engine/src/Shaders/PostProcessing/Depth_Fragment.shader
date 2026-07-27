#version 330 core
in vec2 vUV;
out vec4 FragColor;
uniform sampler2D uDepth;
const float uNear = 0.1;
const float uFar = 100.0;

/// Converts nonlinear depth buffer value back to linear eye-space distance, normalized 0-1
float LinearizeDepth(float depth) {
    float z = depth*2.0 - 1.0;
    float linear = (2.0*uNear*uFar)/(uFar + uNear - z*(uFar - uNear));
    return linear/uFar;
}

void main () {
    float depth = texture(uDepth, vUV).r;
    float linear = LinearizeDepth(depth);
    FragColor = vec4(linear, linear, linear, 1.0);
}
