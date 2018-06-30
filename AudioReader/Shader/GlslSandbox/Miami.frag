#version 330 core

uniform float time;
uniform vec2 offset;
uniform vec2 size;
varying vec2 texcoord;
uniform float lastBeat;
layout(location = 0) out vec4 color;

void main()
{
    float transition_width = 0.1;
    float transition_width2 = transition_width * 0.5;
    float transition_pos = 0.5;
    float pos = texcoord.x;
    if(size.x < size.y) pos = texcoord.y;
    float val = fract(pos * 4);
    float edge1 = 1.0 - smoothstep(0.0, transition_width, val);
    float edge2 = smoothstep(transition_pos - transition_width2 , transition_pos + transition_width2, val);
    float fac = edge1 + edge2;

    //float fac = sin(texcoord.y * 10.0 + time / 5000.0) * 0.5 + 0.5;
    vec3 c = mix(vec3(1.0, 0.0, 1.0), vec3(0.0, 1.0, 1.0), fac);
    color = vec4(c, 1.0);
}
