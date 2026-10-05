#version 330 core

in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uOriginal;   /// original lit scene, bound manually
uniform sampler2D uSceneColor; /// blurred AO, arrives here via the normal chain
uniform float uPower = 1;         /// 1.0 = plain linear multiply, 2.2 = old gamma-space look
uniform float uDirectProtect = 0.0; /// 0 = off, 0.5 to 1.0 = less AO on bright (directly lit) pixels

void main () {
    vec4 aoOverlay = texture(uOriginal, vUV);
    float ao = texture(uSceneColor, vUV).r;
    ao = pow(ao, uPower);

    float lum = dot(aoOverlay.rgb, vec3(0.2126, 0.7152, 0.0722));
    ao = mix(1.0, ao, 1.0/(1.0 + lum*uDirectProtect));

    FragColor = vec4(aoOverlay.rgb*ao, aoOverlay.a);
    // FragColor = vec4(aoOverlay.rgb, 1);
    // FragColor = vec4(vec3(ao), 1.0);
}
