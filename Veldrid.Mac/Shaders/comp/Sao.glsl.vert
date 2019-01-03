#version 100
#ifdef GL_ARB_shading_language_420pack
#extension GL_ARB_shading_language_420pack : require
#endif

varying vec2 oTexCoord;
attribute vec2 iTexCoord;
attribute vec3 iPosition;

void main()
{
    oTexCoord = vec2(iTexCoord.x, 1.0 - iTexCoord.y);
    gl_Position = vec4(iPosition, 1.0);
}

