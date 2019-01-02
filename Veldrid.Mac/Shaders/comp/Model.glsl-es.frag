#version 310 es

layout(binding = 0, std140) uniform ModelMatrix
{
    mat4 uModelMatrix;
} _13;

layout(binding = 2, std140) uniform ProjectionMatrix
{
    mat4 uProjectionMatrix;
} _137;

layout(binding = 1, std140) uniform ViewMatrix
{
    mat4 uViewMatrix;
} _142;

layout(location = 0) in vec3 iPosition;
layout(location = 0) out vec3 oPosition;
layout(location = 1) out vec2 oTexCoord;
layout(location = 1) in vec2 iTexCoord;
layout(location = 2) in vec3 iNormal;
layout(location = 3) in vec3 iTangent;
layout(location = 3) out mat3 oTBN;
layout(location = 2) out vec3 oNormal;
layout(location = 6) out vec3 oVertexPosition;

void main()
{
    vec4 position = _13.uModelMatrix * vec4(iPosition, 1.0);
    oPosition = vec3(position.xyz) / vec3(position.w);
    oTexCoord = vec2(iTexCoord.x, 1.0 - iTexCoord.y);
    vec3 normalW = normalize(vec3((_13.uModelMatrix * vec4(iNormal, 0.0)).xyz));
    vec3 tangentW = normalize(vec3((_13.uModelMatrix * vec4(iTangent, 0.0)).xyz));
    vec3 bitangentW = cross(normalW, tangentW);
    oTBN = mat3(vec3(vec3(tangentW)), vec3(vec3(bitangentW)), vec3(vec3(normalW)));
    oNormal = normalW;
    oVertexPosition = iPosition;
    gl_Position = (_137.uProjectionMatrix * _142.uViewMatrix) * position;
}

