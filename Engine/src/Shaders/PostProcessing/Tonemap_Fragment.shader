#version 330 core

in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uSceneColor;
uniform float uExposure = 1.0;
uniform int uTonemapMode = 1; /// 0 = clamp, 1 = ACES, 2 = Reinhard (luminance)

vec3 ACESFilm (vec3 x) {
    return clamp((x*(2.51*x + 0.03))/(x*(2.43*x + 0.59) + 0.14), 0.0, 1.0);
}

vec3 ReinhardLum (vec3 c) {
    float l = dot(c, vec3(0.2126, 0.7152, 0.0722));
    return c*(1.0/(1.0 + l));
}

float IGN (vec2 p) {
    return fract(52.9829189*fract(dot(p, vec2(0.06711056, 0.00583715))));
}

void main () {
    vec4 src = texture(uSceneColor, vUV);
    vec3 c = src.rgb*uExposure;

    if (uTonemapMode == 1) c = ACESFilm(c);
    else if (uTonemapMode == 2) c = ReinhardLum(c);
    else c = clamp(c, 0.0, 1.0);

    c = pow(c, vec3(1.0/2.2)); /// or the exact sRGB curve
    c += (IGN(gl_FragCoord.xy) - 0.5)/255.0; /// dither, hides 8-bit banding

    FragColor = vec4(c, src.a);
}
