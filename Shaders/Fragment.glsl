#version 330 core

#define ARRAY_SIZE 4

struct Example
{
    vec2 V0;
    vec2 V1;
};

uniform AUniformBlock
{
    Example[ARRAY_SIZE] ArrayOfStructs;
};

in vec2 FTex;

out vec4 fragColor;

void main(void)
{
    if(ArrayOfStructs[2].V1.y == 200.0)
    {
        fragColor = vec4(0.0, 1.0, 0.0, 1.0);
    }
    else
    {
        fragColor = vec4(1.0, 0.0, 0.0, 1.0);
    }
}