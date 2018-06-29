// source: http://glslsandbox.com/e#44063.0

#version 330 core

#extension GL_OES_standard_derivatives : enable

// Julia set

uniform float time;
uniform vec2 mouse;
uniform vec2 resolution;
uniform vec2 offset;
uniform vec2 size;
varying vec2 texcoord;
layout(location = 0) out vec4 color;


vec3 hsv2rgb(vec3 c) {
  vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
  vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
  return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}
	
void main( void ) {
	vec2 newPos = (texcoord * size + offset) * resolution;
	vec2 p = ( newPos - resolution.xy/2.0)/ resolution.y*4.0;
	
	//vec2 c = vec2(5*cosh(time/2.0), 0.7885*sin(time/2.0));
	vec2 c = (mouse-vec2(0.5))*2.0;
	
	float x = p.x, y = p.y;

	int nIter=0;
	for (int i=0; i<=40; i++) {
		float x_temp = x*x-y*y;
		y = 2.0*x*y;
		x = x_temp;
		x += c.x;
		y += c.y;
		nIter = i;
		if (sqrt(x*x+y*y) > 4.0) {
			break;
		}
	}
	
	vec3 col;
	if (nIter!=40) {
		float hueRate = 1.0;
		col = hsv2rgb(vec3((float(nIter)/40.0)*hueRate, 1.0, 0.8));
	}

	color = vec4(col, 1.0);
}
