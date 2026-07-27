#version 330 core

in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uScene;
uniform vec2 uInvResolution;

/// Quality-tier FXAA (based on FXAA 3.11), luma-based edge search
/// along the estimated edge tangent, trimmed subpixel term, offset
/// clamp, and corner-ambiguity bail-out to avoid fattened intersections.

#define FXAA_EDGE_THRESHOLD_MIN 0.0312
#define FXAA_EDGE_THRESHOLD 0.125
#define FXAA_SEARCH_STEPS 10
#define FXAA_SUBPIX_TRIM 0.4
#define FXAA_SUBPIX_TRIM_SCALE 1.0
#define FXAA_SUBPIX_CAP 0.5
#define FXAA_MAX_OFFSET 0.35
#define FXAA_CORNER_AMBIGUITY 0.1

float Luma(vec3 rgb) {
    return dot(rgb, vec3(0.299, 0.587, 0.114));
}

void main () {
    vec2 pos = vUV;
    vec3 rgbM = texture(uScene, pos).rgb;

    vec3 rgbN = textureOffset(uScene, pos, ivec2( 0, -1)).rgb;
    vec3 rgbS = textureOffset(uScene, pos, ivec2( 0,  1)).rgb;
    vec3 rgbE = textureOffset(uScene, pos, ivec2( 1,  0)).rgb;
    vec3 rgbW = textureOffset(uScene, pos, ivec2(-1,  0)).rgb;

    float lumaM = Luma(rgbM);
    float lumaN = Luma(rgbN);
    float lumaS = Luma(rgbS);
    float lumaE = Luma(rgbE);
    float lumaW = Luma(rgbW);

    float lumaMin = min(lumaM, min(min(lumaN, lumaS), min(lumaE, lumaW)));
    float lumaMax = max(lumaM, max(max(lumaN, lumaS), max(lumaE, lumaW)));
    float lumaRange = lumaMax - lumaMin;

    /// Early-out: flat area, skip AA entirely.
    if (lumaRange < max(FXAA_EDGE_THRESHOLD_MIN, lumaMax*FXAA_EDGE_THRESHOLD)) {
        FragColor = vec4(rgbM, 1.0);
        return;
    }

    vec3 rgbNW = textureOffset(uScene, pos, ivec2(-1, -1)).rgb;
    vec3 rgbNE = textureOffset(uScene, pos, ivec2( 1, -1)).rgb;
    vec3 rgbSW = textureOffset(uScene, pos, ivec2(-1,  1)).rgb;
    vec3 rgbSE = textureOffset(uScene, pos, ivec2( 1,  1)).rgb;

    float lumaNW = Luma(rgbNW);
    float lumaNE = Luma(rgbNE);
    float lumaSW = Luma(rgbSW);
    float lumaSE = Luma(rgbSE);

    /// Subpixel aliasing estimate, trimmed so ordinary edges and
    /// corners don't get forced into max blend, only genuine thin
    /// features (hairlines, text) do.
    float lumaL = (lumaN + lumaS + lumaE + lumaW) * 0.25;
    float rangeL = abs(lumaL - lumaM);
    float subpixBlendFinal = clamp((rangeL/lumaRange) - FXAA_SUBPIX_TRIM, 0.0, 1.0);
    subpixBlendFinal = min(FXAA_SUBPIX_CAP, subpixBlendFinal*FXAA_SUBPIX_TRIM_SCALE);

    /// Determine edge orientation (horizontal vs vertical).
    float edgeVert =
        abs((0.25*lumaNW) + (-0.5*lumaN) + (0.25*lumaNE)) +
        abs((0.50*lumaW ) + (-1.0*lumaM) + (0.50*lumaE )) +
        abs((0.25*lumaSW) + (-0.5*lumaS) + (0.25*lumaSE));
    float edgeHorz =
        abs((0.25*lumaNW) + (-0.5*lumaW) + (0.25*lumaSW)) +
        abs((0.50*lumaN ) + (-1.0*lumaM) + (0.50*lumaS )) +
        abs((0.25*lumaNE) + (-0.5*lumaE) + (0.25*lumaSE));

    /// Corner / X-junction bail-out: when both orientations score
    /// nearly equal, the edge direction is ambiguous (two edges
    /// crossing) and blending here is what fattens intersections.
    /// Skip geometric AA, keep only the trimmed subpixel term.
    if (abs(edgeHorz - edgeVert) < FXAA_CORNER_AMBIGUITY*max(edgeHorz, edgeVert)) {
        vec3 rgbCorner = mix(rgbM, (rgbN+rgbS+rgbE+rgbW)*0.25, subpixBlendFinal);
        FragColor = vec4(rgbCorner, 1.0);
        return;
    }

    bool isHorizontal = edgeHorz >= edgeVert;

    /// Pick the two neighbors perpendicular to the edge and figure
    /// out which side has the steeper luma gradient.
    float luma1 = isHorizontal ? lumaN : lumaW;
    float luma2 = isHorizontal ? lumaS : lumaE;
    float gradient1 = luma1 - lumaM;
    float gradient2 = luma2 - lumaM;
    bool is1Steepest = abs(gradient1) >= abs(gradient2);
    float gradientScaled = 0.25*max(abs(gradient1), abs(gradient2));

    float stepLength = isHorizontal ? uInvResolution.y : uInvResolution.x;
    float lumaLocalAvg = 0.0;
    if (is1Steepest) {
        stepLength = -stepLength;
        lumaLocalAvg = 0.5*(luma1 + lumaM);
    } else {
        lumaLocalAvg = 0.5*(luma2 + lumaM);
    }

    vec2 currentUV = pos;
    if (isHorizontal) {
        currentUV.y += stepLength*0.5;
    } else {
        currentUV.x += stepLength*0.5;
    }

    /// Search along the edge tangent in both directions until the
    /// local luma diverges enough from the local average to call it
    /// the edge's end.
    vec2 offset = isHorizontal ? vec2(uInvResolution.x, 0.0) : vec2(0.0, uInvResolution.y);
    vec2 uv1 = currentUV - offset;
    vec2 uv2 = currentUV + offset;

    float lumaEnd1 = Luma(texture(uScene, uv1).rgb) - lumaLocalAvg;
    float lumaEnd2 = Luma(texture(uScene, uv2).rgb) - lumaLocalAvg;
    bool reached1 = abs(lumaEnd1) >= gradientScaled;
    bool reached2 = abs(lumaEnd2) >= gradientScaled;
    bool reachedBoth = reached1 && reached2;

    if (!reached1) uv1 -= offset;
    if (!reached2) uv2 += offset;

    for (int i = 0; i < FXAA_SEARCH_STEPS; i++) {
        if (reachedBoth) break;
        if (!reached1) {
            lumaEnd1 = Luma(texture(uScene, uv1).rgb) - lumaLocalAvg;
            reached1 = abs(lumaEnd1) >= gradientScaled;
            if (!reached1) uv1 -= offset;
        }
        if (!reached2) {
            lumaEnd2 = Luma(texture(uScene, uv2).rgb) - lumaLocalAvg;
            reached2 = abs(lumaEnd2) >= gradientScaled;
            if (!reached2) uv2 += offset;
        }
        reachedBoth = reached1 && reached2;
    }

    float dist1 = isHorizontal ? (pos.x - uv1.x) : (pos.y - uv1.y);
    float dist2 = isHorizontal ? (uv2.x - pos.x) : (uv2.y - pos.y);
    bool isDir1 = dist1 < dist2;
    float distFinal = min(dist1, dist2);
    float edgeThickness = dist1 + dist2;

    float pixelOffset = -distFinal/edgeThickness + 0.5;

    bool isLumaCenterSmaller = lumaM < lumaLocalAvg;
    bool correctVariation = ((isDir1 ? lumaEnd1 : lumaEnd2) < 0.0) != isLumaCenterSmaller;
    float finalOffset = correctVariation ? pixelOffset : 0.0;

    /// Clamp geometric offset: low-confidence edges (near corners,
    /// steep angles) can push pixelOffset toward 0.5, which is what
    /// makes those spots look thick/solid. Cap it.
    finalOffset = min(finalOffset, FXAA_MAX_OFFSET);
    finalOffset = max(finalOffset, subpixBlendFinal);

    vec2 finalUV = pos;
    if (isHorizontal) {
        finalUV.y += finalOffset*stepLength;
    } else {
        finalUV.x += finalOffset*stepLength;
    }

    FragColor = vec4(texture(uScene, finalUV).rgb, 1.0);
}
