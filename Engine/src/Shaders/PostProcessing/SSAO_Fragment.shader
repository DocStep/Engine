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
uniform float uSampleCount;
// uniform float uFalloffPower;

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

/// Interleaved gradient noise
float Noise (vec2 p) {
    return fract(52.9829189*fract(dot(p, vec2(0.06711056, 0.00583715))));
}

void main () {
    float centerRawDepth = texture(uDepth, vUV).r;

    if (LinearizeDepth(centerRawDepth) > uFar*0.99) {
        FragColor = vec4(1.0);
        FragNormal = vec4(normalize(vViewNormal)*0.5 + 0.5, 1.0);
        return;
    }

    vec3 origin = ViewPosFromDepth(vUV);

    vec3 sampledNormal = texture(uNormal, vUV).xyz;
    vec3 normal;

    if (length(sampledNormal) < 0.01) {
        normal = ReconstructNormal(vUV, origin);
    } else {
        normal = normalize(sampledNormal);
    }

    /// Make sure the normal points toward the camera.
    if (dot(normal, -normalize(origin)) < 0.0) {
        normal = -normal;
    }

    /// Build a tangent basis around the surface normal.
    vec3 reference = abs(normal.z) < 0.999
        ? vec3(0.0, 0.0, 1.0)
        : vec3(0.0, 1.0, 0.0);

    vec3 tangent = normalize(cross(reference, normal));
    vec3 bitangent = cross(normal, tangent);

    float baseAngle = Noise(gl_FragCoord.xy)*6.2831853;

    float occlusion = 0.0;
    float validSamples = 0.0;

    int sampleCount = max(int(uSampleCount), 1);

    for (int i = 0; i < sampleCount; i++) {
        float fi = float(i);
        float t = (fi + 0.5)/float(sampleCount);

        /// Uniform-ish hemisphere distribution.
        float phi = baseAngle + fi*GOLDEN_ANGLE;
        float cosTheta = 1.0 - t;
        float sinTheta = sqrt(max(1.0 - cosTheta*cosTheta, 0.0));

        /// Slightly concentrate samples toward the origin.
        float radius = t*t*uRadius;

        vec3 hemisphereDirection =
            tangent*(cos(phi)*sinTheta) +
            bitangent*(sin(phi)*sinTheta) +
            normal*cosTheta;

        vec3 samplePosition = origin + hemisphereDirection*radius;

        /// Project the actual 3D sample position.
        vec4 sampleClip = uProjection*vec4(samplePosition, 1.0);

        if (sampleClip.w <= 0.0) {
            continue;
        }

        vec3 sampleNDC = sampleClip.xyz/sampleClip.w;
        vec2 sampleUV = sampleNDC.xy*0.5 + 0.5;

        if (sampleUV.x < 0.0 || sampleUV.x > 1.0 ||
            sampleUV.y < 0.0 || sampleUV.y > 1.0) {
            continue;
        }

        float sampleDepth = texture(uDepth, sampleUV).r;

        if (sampleDepth >= 1.0) {
            continue;
        }

        vec3 actualPosition = ViewPosFromDepth(sampleUV);

        validSamples += 1.0;

        /// The depth buffer contains geometry at actualPosition.
        /// The generated hemisphere sample is the position we expected
        /// to see at this screen coordinate.
        float depthDifference = samplePosition.z - actualPosition.z;

        /// OpenGL view space looks down -Z.
        /// Actual geometry is in front of our sample when its Z is greater.
        if (depthDifference > uBias) {
            float distance = length(actualPosition - origin);

            if (distance <= uRadius) {
                float rangeCheck = 1.0 - smoothstep(0.0, uRadius, distance);

                float contribution = rangeCheck;

                occlusion += contribution;
            }
        }
    }

    float ao = 1.0;

    if (validSamples > 0.0) {
        float rawAO = occlusion/validSamples;
        ao = 1.0 - clamp(rawAO*uStrength, 0.0, 1.0);
    }

    FragColor = vec4(vec3(ao), 1.0);
    FragNormal = vec4(normal*0.5 + 0.5, 1.0);
}
