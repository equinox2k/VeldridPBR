#version 450

layout(set = 0, binding = 0) uniform State
{
    mat4 modelMatrix;
    mat4 viewMatrix;
    mat4 projectionMatrix;
} state;

layout(location = 0) in vec3 iPosition;
layout(location = 1) in vec2 iTexCoord;
layout(location = 2) in vec3 iNormal;
layout(location = 3) in vec3 iTangent;

layout(location = 0) out vec3 oPosition;
layout(location = 1) out vec2 oTexCoord;
layout(location = 2) out vec3 oNormal;
layout(location = 3) out mat3 oTBN;
layout(location = 6) out vec3 oVertexPosition;

void main()
{
    vec4 position = state.modelMatrix * vec4(iPosition, 1.0);
    oPosition = vec3(position.xyz) / vec3(position.w);
    oTexCoord = vec2(iTexCoord.x, 1.0 - iTexCoord.y);
    vec3 normalW = normalize(vec3((state.modelMatrix * vec4(iNormal, 0.0)).xyz));
    vec3 tangentW = normalize(vec3((state.modelMatrix * vec4(iTangent, 0.0)).xyz));
    vec3 bitangentW = cross(normalW, tangentW);
    oTBN = mat3(vec3(tangentW), vec3(bitangentW), vec3(normalW));
    oNormal = normalW;
    oVertexPosition = iPosition;
    gl_Position = (state.projectionMatrix * state.viewMatrix) * position;
}