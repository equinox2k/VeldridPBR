#version 100
#ifdef GL_ARB_shading_language_420pack
#extension GL_ARB_shading_language_420pack : require
#endif

struct NormalMatrix
{
    mat4 uNormalMatrix;
};

uniform NormalMatrix _14;

struct ProjectionMatrix
{
    mat4 uProjectionMatrix;
};

uniform ProjectionMatrix _39;

struct ViewMatrix
{
    mat4 uViewMatrix;
};

uniform ViewMatrix _44;

struct ModelMatrix
{
    mat4 uModelMatrix;
};

uniform ModelMatrix _50;

varying vec3 oNormal;
attribute vec3 iNormal;
attribute vec3 iPosition;

void main()
{
    oNormal = normalize(_14.uNormalMatrix * vec4(iNormal, 0.0)).xyz;
    gl_Position = ((_39.uProjectionMatrix * _44.uViewMatrix) * _50.uModelMatrix) * vec4(iPosition, 1.0);
    gl_Position.z = 2.0 * gl_Position.z - gl_Position.w;
}

