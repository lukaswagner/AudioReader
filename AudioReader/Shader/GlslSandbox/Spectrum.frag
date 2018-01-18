#version 330 core

#define ArraySize %AUDIODATASIZE%

uniform float audioData[ArraySize];
varying vec2 texcoord;
layout(location = 0) out vec4 color;

void main()
{
    int pos = int(clamp(texcoord.x * ArraySize, 0f, ArraySize - 1f));
    float value = audioData[pos];
    color = value*50 > texcoord.y ? vec4(vec3(1.0), 1.0) : vec4(vec3(0.0), 1.0);
}
