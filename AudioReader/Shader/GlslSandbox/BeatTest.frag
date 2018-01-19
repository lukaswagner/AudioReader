#version 330 core

uniform float beat;
layout(location = 0) out vec4 color;

void main()
{
    color = vec4(vec3(clamp((1000 - beat) / 1000, 0.0, 1.0)), 1.0);
}
