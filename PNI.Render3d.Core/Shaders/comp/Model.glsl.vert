#version 100
#ifdef GL_ARB_shading_language_420pack
#extension GL_ARB_shading_language_420pack : require
#endif

struct ModelMatrix
{
    mat4 uModelMatrix;
};

uniform ModelMatrix _13;

struct ProjectionMatrix
{
    mat4 uProjectionMatrix;
};

uniform ProjectionMatrix _130;

struct ViewMatrix
{
    mat4 uViewMatrix;
};

uniform ViewMatrix _135;

attribute vec3 iPosition;
varying vec3 oPosition;
varying vec2 oTexCoord;
attribute vec2 iTexCoord;
attribute vec3 iNormal;
attribute vec3 iTangent;
varying mat3 oTBN;
varying vec3 oNormal;
varying vec3 oVertexPosition;

void main()
{
    vec4 position = _13.uModelMatrix * vec4(iPosition, 1.0);
    oPosition = vec3(position.xyz) / vec3(position.w);
    oTexCoord = iTexCoord;
    vec3 normalW = normalize(vec3((_13.uModelMatrix * vec4(iNormal, 0.0)).xyz));
    vec3 tangentW = normalize(vec3((_13.uModelMatrix * vec4(iTangent, 0.0)).xyz));
    vec3 bitangentW = cross(normalW, tangentW);
    oTBN = mat3(vec3(vec3(tangentW)), vec3(vec3(bitangentW)), vec3(vec3(normalW)));
    oNormal = normalW;
    oVertexPosition = iPosition;
    gl_Position = (_130.uProjectionMatrix * _135.uViewMatrix) * position;
    gl_Position.z = 2.0 * gl_Position.z - gl_Position.w;
}

