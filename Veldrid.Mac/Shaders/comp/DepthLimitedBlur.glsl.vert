#version 100
#ifdef GL_ARB_shading_language_420pack
#extension GL_ARB_shading_language_420pack : require
#endif

struct Size
{
    vec2 uSize;
};

uniform Size _17;

varying vec2 oTexCoord;
attribute vec2 iTexCoord;
varying vec2 oInvSize;
attribute vec3 iPosition;

void main()
{
    oTexCoord = iTexCoord;
    oInvSize = vec2(1.0) / _17.uSize;
    gl_Position = vec4(iPosition, 1.0);
}

