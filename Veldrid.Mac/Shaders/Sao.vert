#version 450

layout(location = 0) in vec3 iPosition;
layout(location = 1) in vec2 iTexCoord;

layout(location = 0) out vec2 oTexCoord;

void main()
{
    oTexCoord = vec2(iTexCoord.x, 1.0 - iTexCoord.y);
    gl_Position = vec4(iPosition, 1.0);
    gl_Position.y = -gl_Position.y;
}

