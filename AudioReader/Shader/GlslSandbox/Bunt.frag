#version 330 core

uniform float time;
uniform vec2 offset;
uniform vec2 size;
varying vec2 texcoord;
layout(location = 0) out vec4 color;

vec3 hsv2rgb(vec3 c) {
  vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
  vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
  return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

float map( float value, float inMin, float inMax, float outMin, float outMax ) 
{
    return ( (value - inMin) / ( inMax - inMin ) * ( outMax - outMin ) ) + outMin; 
}

vec2 hash( vec2 p )
{
	p = vec2( dot(p,vec2(127.1,311.7)),
			  dot(p,vec2(269.5,183.3)) );

	return -1.0 + 2.0*fract(sin(p)*43758.5453123);
}

float level=1.;
float noise( in vec2 p )
{
    vec2 i = floor( p );
    vec2 f = fract( p );
	
	vec2 u = f*f*f*(10. + f*(6.*f - 15.));


    float t = 1.0*time * 0.0003;
    mat2 R = mat2(cos(t),-sin(t),sin(t),cos(t));
//    if (mod(i.x+i.y,2.)==0.) R=-R;

    return 2.*mix( mix( dot( hash( i + vec2(0,0) )*R, (f - vec2(0,0)) ), 
                     dot( hash( i + vec2(1,0) )*R,(f - vec2(1,0)) ), u.x),
                mix( dot( hash( i + vec2(0,1) )*R,(f - vec2(0,1)) ), 
                     dot( hash( i + vec2(1,1) )*R, (f - vec2(1,1)) ), u.x), u.y);
}

float Mnoise(in vec2 uv ) {
    return noise(uv);                      // base turbulence
  //return -1. + 2.* (1.-abs(noise(uv)));  // flame like
    //return -1. + 2.* (abs(noise(uv)));     // cloud like
}

float turb( in vec2 uv )
{ 	float f = 0.0;
	
 level=1.;
    mat2 m = mat2( 1.6,  1.2, -1.2,  1.6 );
    f  = 0.5000*Mnoise( uv ); uv = m*uv; level++;
	//f += 0.2500*Mnoise( uv ); uv = m*uv; level++;
	//f += 0.1250*Mnoise( uv ); uv = m*uv; level++;
	//f += 0.0625*Mnoise( uv ); uv = m*uv; level++;
	return f/.9375; 
}
// -----------------------------------------------

void main( void ) {
	
  vec2 uv = (texcoord * size + offset) * vec2(0.12, 0.6);
  color = vec4(uv, 0.0, 1.0);
	float f; 
  f = Mnoise( 5.*uv );
	color = vec4(vec3(f), 1.0);
  f = turb( 5.*uv ) * 1.5 + 0.5;
  f = clamp(f, 0.0, 1.0);
  //f = map(f, 0.0, 1.0, 0.45, 0.73);
	color = vec4(vec3(f), 1.0);
  vec3 c = mix(vec3(0.0, 0.0, 1.0), vec3(0.0, 1.0, 1.0), f);
  vec3 hsv = vec3(f, 1.0, 1.0);
  vec3 rgb = hsv2rgb(hsv);
  color = vec4(rgb, 1.0);
  //color = vec4(.5 + .5* f);
  //color = mix(vec4(0,0,.3,1),vec4(1.3),gl_FragColor); 
}
