#version 330 core

uniform float beat;
layout(location = 0) out vec4 color;

void main()
{
    color = vec4(vec3(beat), 1.0);
}
