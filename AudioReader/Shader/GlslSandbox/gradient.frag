#version 330 core

uniform float time;
varying vec2 texcoord;
layout(location = 0) out vec4 color;

vec3 hsv2rgb(vec3 c) {
  vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
  vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
  return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void main()
{
    vec3 c = hsv2rgb(vec3(fract(texcoord.x * 0.4 + time / 10000.0), 1.0, 1.0));	
    color = vec4(c, 1.0);	
}
