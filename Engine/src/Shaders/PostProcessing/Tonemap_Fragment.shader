#version 330 core

in vec2 vUV;

out vec4 FragColor;

uniform sampler2D uSceneColor;
uniform sampler2D uDepth;
uniform float uExposure;
uniform int uTonemapMode;


vec3 ACESFilm (vec3 x) {
    return clamp((x*(2.51*x + 0.03))/(x*(2.43*x + 0.59) + 0.14), 0.0, 1.0);
}

vec3 ReinhardLum (vec3 color) {
    float l = dot(color, vec3(0.2126, 0.7152, 0.0722));
    return color*(1.0/(1.0 + l));
}

/// Reinhard extended: white point makes bright values reach pure white
vec3 ReinhardExt (vec3 color) {
    const float W = 8.0;
    float l = dot(color, vec3(0.2126, 0.7152, 0.0722));
    float lm = l*(1.0 + l/(W*W))/(1.0 + l);
    return color*(lm/max(l, 1e-5));
}

/// Uncharted 2 (Hable): strong filmic contrast, warm highlights
vec3 Hable (vec3 x) {
    const float A = 0.15, B = 0.50, C = 0.10, D = 0.20, E = 0.02, F = 0.30;
    return ((x*(A*x + C*B) + D*E)/(x*(A*x + B) + D*F)) - E/F;
}
vec3 HableMap (vec3 color) {
    const float W = 11.2;
    return Hable(color*2.0)/Hable(vec3(W));
}


/// Khronos PBR Neutral: keeps albedo colors almost unchanged, good for a game
vec3 PBRNeutral (vec3 color) {
    const float startCompression = 0.8 - 0.04;
    const float desaturation = 0.15;
    float x = min(color.r, min(color.g, color.b));
    float offset = x < 0.08 ? x - 6.25*x*x : 0.04;
    color -= offset;
    float peak = max(color.r, max(color.g, color.b));
    if (peak < startCompression) return color;
    float d = 1.0 - startCompression;
    float newPeak = 1.0 - d*d/(peak + d - startCompression);
    color *= newPeak/peak;
    float g = 1.0 - 1.0/(desaturation*(peak - newPeak) + 1.0);
    return mix(color, vec3(newPeak), g);
}

float IGN (vec2 p) {
    return fract(52.9829189*fract(dot(p, vec2(0.06711056, 0.00583715))));
}

void main () {
    vec4 sceneColor = texture(uSceneColor, vUV);
    vec3 color = sceneColor.rgb*uExposure;
    float d = texture(uDepth, vUV).r;

    // Skybox leaves depth at the clear value because it does not write depth.
    //if (d < 1.0) {
    if (uTonemapMode == 1) color = PBRNeutral(color);
    else if (uTonemapMode == 2) color = ACESFilm(color);
    else if (uTonemapMode == 3) color = ReinhardLum(color);
    else if (uTonemapMode == 4) color = ReinhardExt(color);
    else if (uTonemapMode == 5) color = Hable(color);
    else if (uTonemapMode == 6) color = HableMap(color);
    else color = clamp(color, 0.0, 1.0);

    color = pow(max(color, vec3(0.0)), vec3(1.0/2.2)); /// or the exact sRGB curve
    color += (IGN(gl_FragCoord.xy) - 0.5)/255.0; /// dither, hides 8-bit banding
    //} else {
    //    // Keep sky HDR values un-tonemapped, but convert linear color for display.
    //    color = pow(max(color, vec3(0.0)), vec3(1.0/2.2));
    //}

    const float saturation = 0.9;
    float luma = dot(color, vec3(0.2126, 0.7152, 0.0722));
    color = mix(vec3(luma), color, saturation); /// 1.0 = original, 0.0 = grayscale

    FragColor = vec4(color, sceneColor.a);
}
