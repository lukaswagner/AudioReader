#version 330 core

uniform sampler2D albumArt;
uniform float trackProgress;
uniform float time;
uniform int isPlaying;
varying vec2 texcoord;
layout(location = 0) out vec4 color;

vec3 hsv2rgb(vec3 c) {
  vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
  vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
  return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void main()
{
    /*vec4 artColor = texture2D(albumArt, texcoord);
    vec4 artGray = vec4(vec3(dot(artColor.rgb, vec3(0.2126, 0.7152, 0.0722))), 1.0);
    vec4 art = mix(artGray, artColor, isPlaying);
    vec4 progress = mix(vec4(vec3(0.0), 1.0), vec4(1.0), int(trackProgress > texcoord.x));/**/
	
	vec3 c = hsv2rgb(vec3(fract(texcoord.x + time / 5000.0), 1.0, 1.0));/**/
	/*vec2 pos = gl_FragCoord.xy;
	float p = pos.x + pos.y * 5.0;
	float m = 5.0 * 30.0;/**/
	
	
	
	
    color = vec4(c, 1.0);
	
}
