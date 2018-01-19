attribute vec3 position;
varying vec2 texcoord;

void main() {
    texcoord = position.xy * vec2(0.5) + vec2(0.5);
    gl_Position = vec4(position, 1.0);
}
