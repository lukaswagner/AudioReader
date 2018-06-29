#version 330 core

#define ArraySize %AUDIODATASIZE%

uniform float audioData[ArraySize];
uniform vec2 offset;
uniform vec2 size;
varying vec2 texcoord;
layout(location = 0) out vec4 color;

void main()
{
    vec2 newPos = texcoord * size + offset;
    int pos = int(clamp(newPos.x * ArraySize, 0.0, ArraySize - 1.0));
    float value = audioData[pos];
    color = value*50 > newPos.y ? vec4(vec3(1.0), 1.0) : vec4(vec3(0.0), 1.0);
}
