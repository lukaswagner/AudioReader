#version 330 core

uniform float time;
varying vec2 texcoord;
layout(location = 0) out vec4 color;

void main()
{
    float val = sin(texcoord.y * 10.0 + time / 5000.0) * 0.5 + 0.5;
    val = smoothstep(0.2, 0.8, val);
    vec3 c = mix(vec3(1., 1., .9), vec3(0.0, 0.0, 0.0), val);
    color = vec4(1.0);
}
