#version 330 core

uniform float audioData[128];
varying vec2 texcoord;
layout(location = 0) out vec4 color;

void main()
{
    int pos = int(clamp(texcoord.x * 128f, 0f, 127f));
    float value = audioData[pos];
    color = value*50 > texcoord.y ? vec4(vec3(1.0), 1.0) : vec4(vec3(0.0), 1.0);
}
