#version 330 core

uniform float time;
varying vec2 texcoord;
layout(location = 0) out vec4 color;

void main( void ) {
    color = vec4(vec3(0.), 1.0); 
}

