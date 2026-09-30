#version 330 core

layout (location = 0) out vec4 FragColor;
layout (location = 1) out vec4 FragNormal;

in vec2 vUV;
in vec3 vViewNormal;

uniform sampler2D uDepth;
uniform sampler2D uNormal;
uniform mat4 uProjection;
uniform mat4 uInvProjection;
uniform vec2 uTexelSize;
uniform float uRadius;
uniform float uBias;
uniform float uStrength;
uniform float uNear;
uniform float uFar;
// uniform float uFalloffPower;

const int SAMPLE_COUNT = 32;
const float GOLDEN_ANGLE = 2.39996323;


vec3 ViewPosFromDepth (vec2 uv) {
    float z = texture(uDepth, uv).r*2.0 - 1.0;
    vec4 clip = vec4(uv*2.0 - 1.0, z, 1.0);
    vec4 view = uInvProjection*clip;
    return view.xyz/view.w;
}

float LinearizeDepth (float rawDepth) {
    float z = rawDepth*2.0 - 1.0;
    return (2.0*uNear*uFar)/(uFar + uNear - z*(uFar - uNear));
}

vec3 ReconstructNormal (vec2 uv, vec3 origin) {
    vec3 posRight = ViewPosFromDepth(uv + vec2(uTexelSize.x, 0.0));
    vec3 posLeft  = ViewPosFromDepth(uv - vec2(uTexelSize.x, 0.0));
    vec3 posUp    = ViewPosFromDepth(uv + vec2(0.0, uTexelSize.y));
    vec3 posDown  = ViewPosFromDepth(uv - vec2(0.0, uTexelSize.y));

    vec3 ddx = (abs(posRight.z - origin.z) < abs(posLeft.z - origin.z))
        ? posRight - origin : origin - posLeft;
    vec3 ddy = (abs(posUp.z - origin.z) < abs(posDown.z - origin.z))
        ? posUp - origin : origin - posDown;

    return normalize(cross(ddy, ddx));
}

/// Interleaved gradient noise: smoother pattern than sin-hash, blurs out cleanly
float Noise (vec2 p) {
    return fract(52.9829189*fract(dot(p, vec2(0.06711056, 0.00583715))));
}

void main () {
    float centerRawDepth = texture(uDepth, vUV).r;
    if (LinearizeDepth(centerRawDepth) > uFar*0.99) {
        FragColor = vec4(1.0);
        return;
    }

    vec3 origin = ViewPosFromDepth(vUV);
    vec3 normal = normalize(texture(uNormal, vUV).xyz);

    /// view-space radius -> screen-space radius, never below ~1.5 texels
    vec4 offsetClip = uProjection*vec4(origin.xy + vec2(uRadius, 0.0), origin.z, 1.0);
    vec4 originClip = uProjection*vec4(origin, 1.0);
    vec2 offsetUV = (offsetClip.xy/offsetClip.w)*0.5 + 0.5;
    vec2 originUV = (originClip.xy/originClip.w)*0.5 + 0.5;
    float screenRadius = clamp(length(offsetUV - originUV), uTexelSize.x*1.5, 0.25);

    float scaledBias = uBias*max(-origin.z, 1.0)*0.01;
    float baseAngle = Noise(gl_FragCoord.xy)*6.2831853;

    float occlusion = 0.0;
    float validSamples = 0.0;

    for (int i = 0; i < SAMPLE_COUNT; i++) {
        float t = (float(i) + 0.5)/float(SAMPLE_COUNT);
        float angle = baseAngle + float(i)*GOLDEN_ANGLE;
        vec2 dir = vec2(cos(angle), sin(angle));
        vec2 sampleUV = vUV + dir*screenRadius*t;

        if (sampleUV.x < 0.0 || sampleUV.x > 1.0 || sampleUV.y < 0.0 || sampleUV.y > 1.0) continue;
        if (LinearizeDepth(texture(uDepth, sampleUV).r) > uFar*0.99) continue;

        vec3 samplePos = ViewPosFromDepth(sampleUV);
        vec3 toSample = samplePos - origin;
        float distSq = dot(toSample, toSample);
        float distance = sqrt(distSq);
        float height = dot(toSample, normal) - scaledBias;
        float rangeCheck = 1.0 - smoothstep(uRadius*0.5, uRadius, distance);

        if (height > 0.0) {
            float contribution = height/(distance + 0.001);
            occlusion += contribution*rangeCheck;
        }

        validSamples += 1.0;
    }

    float ao = (validSamples > 0.0)
        ? 1.0 - clamp((occlusion/validSamples)*uStrength, 0.0, 1.0)
        : 1.0;

    // ao = (occlusion/validSamples)*uStrength;
    
    FragColor = vec4(vec3(ao), 1.0);
    FragNormal = vec4(normalize(vViewNormal), 1.0);
}
