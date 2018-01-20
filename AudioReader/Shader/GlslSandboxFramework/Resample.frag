#version 330

uniform sampler2D texture;
uniform vec2 originOffset;
uniform vec2 originSize;
varying vec2 texcoord;
layout(location = 0) out vec3 color;

void main()
{
    color = texture2D(texture, mix(originOffset, originOffset + originSize, texcoord)).rgb;
}
