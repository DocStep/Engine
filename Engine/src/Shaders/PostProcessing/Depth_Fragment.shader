#version 330 core
in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uScene;
uniform sampler2D uDepth;
uniform float uNear;
uniform float uFar;

float LinearizeDepth (float d) {
    /// d is in [0,1] NDC depth from a standard perspective projection
    float z = d*2f - 1f;
    return (2f*uNear*uFar)/(uFar + uNear - z*(uFar - uNear));
}

void main () {
    float raw = texture(uDepth, vUV).r;
    float linear = LinearizeDepth(raw);
    float normalized = linear/uFar;

    FragColor = vec4(vec3(normalized), 1f);
}
