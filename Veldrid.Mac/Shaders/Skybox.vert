#version 450

layout(set = 0, binding = 0) uniform InverseModelViewMatrix
{
    mat3 uInverseModelViewMatrix;
};

layout(set = 0, binding = 0) uniform InverseProjectionMatrix
{
    mat4 uInverseProjectionMatrix;
};

layout(location = 0) in vec3 iPosition;

layout(location = 0) out vec3 oViewDirection;

void main()
{
    oViewDirection = uInverseModelViewMatrix * (uInverseProjectionMatrix * vec4(iPosition, 1.0)).xyz;
    gl_Position = vec4(iPosition, 1.0);
    gl_Position.y = -gl_Position.y;
}