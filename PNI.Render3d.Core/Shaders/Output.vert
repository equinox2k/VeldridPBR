#version 450

layout(set = 0, binding = 0) uniform InverseModelViewMatrix
{
    mat4 uInverseModelViewMatrix;
};

layout(set = 0, binding = 1) uniform InverseProjectionMatrix
{
    mat4 uInverseProjectionMatrix;
};

layout(location = 0) in vec3 iPosition;
layout(location = 1) in vec2 iTexCoord;

layout(location = 0) out vec2 oTexCoord;
layout(location = 1) out vec3 oViewDirection;

void main() 
{
    oViewDirection = (uInverseModelViewMatrix * uInverseProjectionMatrix * vec4(iPosition, 1.0)).xyz;
    oTexCoord = vec2(iTexCoord.x, 1.0 - iTexCoord.y);
    gl_Position = vec4(iPosition, 1.0);
}