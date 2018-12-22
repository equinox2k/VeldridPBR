#version 450

layout(set = 0, binding = 0) uniform Size
{
    vec2 uSize;
};

layout(location = 0) in vec3 iPosition;
layout(location = 1) in vec2 iTexCoord;

layout(location = 0) out vec2 oTexCoord;
layout(location = 1) out vec2 oInvSize;

void main() 
{
    oTexCoord = iTexCoord;
    oInvSize = 1.0 / uSize;
    gl_Position =  vec4(iPosition, 1.0);
}
