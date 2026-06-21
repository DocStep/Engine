#version 330 core

in vec2 vScreenPos;
out vec4 FragColor;

uniform mat4 uInvView;
uniform mat4 uInvProjection;
uniform sampler2D uSkyboxTexture;
uniform float uBlurScale;

const float PI = 3.14159265359;

/// Converts a world-space direction into equirectangular UV coords.
vec2 DirectionToEquirectUV (vec3 dir) {
    float u = 0.5 + atan(dir.z, dir.x)/(2.0*PI);
    float v = 0.5 + asin(clamp(dir.y, -1.0, 1.0))/PI;
    return vec2(u, v);
}

/// Narkowicz ACES approximation - cheap, good highlight rolloff.
vec3 Tonemap (vec3 color) {
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    return clamp((color*(a*color + b))/(color*(c*color + d) + e), 0.0, 1.0);
}

void main () {
    vec4 clipPos = vec4(vScreenPos, 1.0, 1.0);
    vec4 viewPos = uInvProjection*clipPos;
    viewPos /= viewPos.w;

    vec3 worldDir = normalize((uInvView*vec4(viewPos.xyz, 0.0)).xyz);

    vec2 uv = DirectionToEquirectUV(worldDir);
    vec3 color = textureLod(uSkyboxTexture, uv, uBlurScale).rgb;

    color = Tonemap(color);
    color = pow(color, vec3(1.0/2.2)); /// gamma correction
    
    FragColor = vec4(color, 1.0);
}
