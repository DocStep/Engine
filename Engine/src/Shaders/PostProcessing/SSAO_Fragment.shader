#version 330 core

in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uDepth;
uniform mat4 uInvProjection;
uniform vec2 uTexelSize;
uniform float uRadius;
uniform float uBias;
uniform float uStrength;

vec3 ViewPosFromDepth (vec2 uv) {
    float z = texture(uDepth, uv).r*2.0 - 1.0;
    vec4 clip = vec4(uv*2.0 - 1.0, z, 1.0);
    vec4 view = uInvProjection*clip;
    return view.xyz/view.w;
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
    vec3 origin = ViewPosFromDepth(vUV);
    vec3 normal = ReconstructNormal(vUV, origin);
    // vec3 normal = normalize(cross(dFdy(origin), dFdx(origin)))

    float rand = fract(sin(dot(vUV, vec2(12.9898, 78.233)))*43758.5453);
    float angle = rand*6.2831853;
    float c = cos(angle), s = sin(angle);
    mat2 rot = mat2(c, -s, s, c);

    vec2 offsets[4] = vec2[](
        vec2(1.0, 0.0), vec2(-1.0, 0.0),
        vec2(0.0, 1.0), vec2(0.0, -1.0)
    );
    float distances[4] = float[](6.0, 8.0, 10.0, 12.0);

    float occlusion = 0.0;
    for (int i = 0; i < 4; i++) {
        vec2 rotatedOffset = rot*offsets[i];
        vec2 sampleUV = vUV + rotatedOffset*uTexelSize*distances[i];
        vec3 samplePos = ViewPosFromDepth(sampleUV);

        vec3 toSample = samplePos - origin;
        float dist = length(toSample);
        float nDotS = max(dot(normal, normalize(toSample)), 0.0);

        float rangeCheck = smoothstep(0.0, 1.0, uRadius/max(dist, 0.0001));
        occlusion += (dist > uBias ? nDotS*rangeCheck : 0.0);
    }

    float ao = 1.0 - clamp(occlusion/4.0*uStrength, 0.0, 1.0);
    FragColor = vec4(vec3(ao), 1.0); /// AO only, no scene sampling
}