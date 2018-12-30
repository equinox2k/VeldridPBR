#version 450

layout(set = 0, binding = 0) uniform ModelMatrix
{
    mat4 uModelMatrix;
};

layout(set = 0, binding = 1) uniform ViewMatrix
{
    mat4 uViewMatrix;
};

layout(set = 0, binding = 2) uniform ProjectionMatrix
{
    mat4 uProjectionMatrix;
};

layout(set = 0, binding = 3) uniform NormalMatrix
{
    mat4 uNormalMatrix;
};

layout(location = 0) in vec3 iPosition;
layout(location = 1) in vec3 iNormal;

layout(location = 0) out vec3 oNormal;

void main() 
{
    oNormal = normalize(uNormalMatrix * vec4(iNormal, 0.0)).xyz;
    gl_Position = uProjectionMatrix * uViewMatrix * uModelMatrix * vec4(iPosition, 1.0);
    gl_Position.y = -gl_Position.y;
}
