#version 100
#ifdef GL_ARB_shading_language_420pack
#extension GL_ARB_shading_language_420pack : require
#endif

struct InverseModelViewMatrix
{
    mat4 uInverseModelViewMatrix;
};

uniform InverseModelViewMatrix _14;

struct InverseProjectionMatrix
{
    mat4 uInverseProjectionMatrix;
};

uniform InverseProjectionMatrix _22;

varying vec3 oViewDirection;
attribute vec3 iPosition;
varying vec2 oTexCoord;
attribute vec2 iTexCoord;

void main()
{
    oViewDirection = ((_14.uInverseModelViewMatrix * _22.uInverseProjectionMatrix) * vec4(iPosition, 1.0)).xyz;
    oTexCoord = vec2(iTexCoord.x, 1.0 - iTexCoord.y);
    gl_Position = vec4(iPosition, 1.0);
}

