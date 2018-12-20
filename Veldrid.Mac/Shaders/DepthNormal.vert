#version 450

layout(set = 0, binding = 0) uniform State
{
    mat4 modelMatrix;
    mat4 viewMatrix;
    mat4 projectionMatrix;
    mat3 normalMatrix;
} state;

layout(location = 0) in vec3 iPosition;
layout(location = 1) in vec3 iNormal;

layout(location = 0) out vec3 oNormal;

void main() 
{
    oNormal = normalize(state.normalMatrix * iNormal);
    gl_Position = state.projectionMatrix * state.viewMatrix * state.modelMatrix * vec4(iPosition, 1.0);
}
