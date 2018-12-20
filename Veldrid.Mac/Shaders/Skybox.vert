#version 450

layout(set = 0, binding = 0) uniform State
{
    mat3 inverseModelViewMatrix;
    mat4 inverseProjectionMatrix;
} state;

layout(location = 0) in vec3 iPosition;

layout(location = 0) out vec3 oViewDirection;

void main()
{
    oViewDirection = state.inverseModelViewMatrix * (state.inverseProjectionMatrix * vec4(iPosition, 1.0)).xyz;
    gl_Position = vec4(iPosition, 1.0);
}