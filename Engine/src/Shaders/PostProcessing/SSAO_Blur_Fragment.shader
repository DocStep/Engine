#version 330 core

in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uSceneColor; /// this is the AO buffer here, not the lit scene
uniform sampler2D uDepth;
uniform vec2 uTexelSize;
uniform float uNear;
uniform float uFar;
uniform float uDepthThreshold; /// relative, try 0.05


float LinearizeDepth (float rawDepth) {
    float z = rawDepth*2.0 - 1.0;
    return (2.0*uNear*uFar)/(uFar + uNear - z*(uFar - uNear));
}

void main () {
    float centerDepth = LinearizeDepth(texture(uDepth, vUV).r);

    float sum = 0.0;
    float weightSum = 0.0;

    for (int x = -2; x <= 2; x++) {
        for (int y = -2; y <= 2; y++) {
            vec2 uv = vUV + vec2(x, y)*uTexelSize;
            float d = LinearizeDepth(texture(uDepth, uv).r);

            /// 1 at same depth, 0 once the difference passes the threshold
            float w = max(0.0, 1.0 - abs(d - centerDepth)/(centerDepth*uDepthThreshold));

            sum += texture(uSceneColor, uv).r*w;
            weightSum += w;
        }
    }

    FragColor = vec4(vec3(sum/max(weightSum, 0.0001)), 1.0);
}