#version 330 core

in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uDepth;
uniform mat4 uProjection;
uniform mat4 uInvProjection;
uniform vec2 uTexelSize;
uniform float uRadius;
uniform float uBias;
uniform float uStrength;
uniform float uNear;
uniform float uFar;
// uniform float uFalloffPower;

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

void main () {
    float centerRawDepth = texture(uDepth, vUV).r;
    float centerLinearDepth = LinearizeDepth(centerRawDepth);
    if (centerLinearDepth > uFar*0.99) {
        FragColor = vec4(1.0);
        return;
    }

    vec3 origin = ViewPosFromDepth(vUV);
    vec3 normal = ReconstructNormal(vUV, origin);

    float rand = fract(sin(dot(vUV, vec2(12.9898, 78.233)))*43758.5453);
    float angle = rand*6.2831853;
    float c = cos(angle), s = sin(angle);
    mat2 rot = mat2(c, -s, s, c);

    vec2 offsets[4] = vec2[](
        vec2(1.0, 0.0), vec2(-1.0, 0.0),
        vec2(0.0, 1.0), vec2(0.0, -1.0)
    );
    float radii[4] = float[](0.5, 0.7, 0.85, 1.0);

    vec4 offsetViewPos = vec4(origin.xy + vec2(uRadius, 0.0), origin.z, 1.0);
    vec4 offsetClip = uProjection*offsetViewPos;
    vec2 offsetUV = (offsetClip.xy/offsetClip.w)*0.5 + 0.5;
    vec4 originClip = uProjection*vec4(origin, 1.0);
    vec2 originUV = (originClip.xy/originClip.w)*0.5 + 0.5;
    float screenRadius = clamp(length(offsetUV - originUV), 0.0001, 0.5);

    float scaledBias = uBias*max(-origin.z, 1.0)*0.01;

    float occlusion = 0.0;
    float validSamples = 0.0;

    for (int i = 0; i < 4; i++) {
        vec2 rotatedOffset = rot*offsets[i];
        vec2 sampleUV = vUV + rotatedOffset*screenRadius*radii[i];

        float sampleRawDepth = texture(uDepth, sampleUV).r;
        float sampleLinearDepth = LinearizeDepth(sampleRawDepth);
        if (sampleLinearDepth > uFar*0.99) continue;

        vec3 samplePos = ViewPosFromDepth(sampleUV);
        vec3 toSample = samplePos - origin;
        float dist = length(toSample);
        float nDotS = max(dot(normal, normalize(toSample)), 0.0);

        float rangeCheck = smoothstep(0.0, 1.0, uRadius/max(dist, 0.0001));
        //rangeCheck = pow(rangeCheck, uFalloffPower); /// sharper falloff between objects

        occlusion += (dist > scaledBias ? nDotS*rangeCheck : 0.0);
        validSamples += 1.0;
    }

    float ao = (validSamples > 0.0)
        ? 1.0 - clamp((occlusion/validSamples)*uStrength, 0.0, 1.0)
        : 1.0;

    FragColor = vec4(vec3(ao), 1.0);
}