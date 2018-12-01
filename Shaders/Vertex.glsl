#version 330 core

in vec2 Position;
in vec2 VTex;

out vec2 FTex;

void main(void)
{
    FTex = VTex;
    gl_Position = vec4(Position.x, Position.y, 0.5, 1.0f);
}