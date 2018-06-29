#version 330 core

uniform sampler2D albumArt;
uniform float trackProgress;
uniform float lastMouse;
uniform int isPlaying;
uniform vec2 offset;
uniform vec2 size;
varying vec2 texcoord;
layout(location = 0) out vec4 color;

void main()
{
    vec2 newPos = texcoord * size + offset;
    vec4 artColor = texture2D(albumArt, newPos);
    vec4 artGray = vec4(vec3(dot(artColor.rgb, vec3(0.2126, 0.7152, 0.0722))), 1.0);
    vec4 art = mix(artGray, artColor, isPlaying);
    vec4 progress = mix(vec4(vec3(0.0), 1.0), vec4(1.0), int(trackProgress > newPos.x));
    color = mix(art, progress, int(newPos.y < 0.1 && lastMouse < 3000));
}
