#version 100
#ifdef GL_ARB_shading_language_420pack
#extension GL_ARB_shading_language_420pack : require
#endif

varying vec2 oTexCoord;
attribute vec2 iTexCoord;
attribute vec3 iPosition;

void main()
{
    oTexCoord = iTexCoord;
    gl_Position = vec4(iPosition, 1.0);
    gl_Position.z = 2.0 * gl_Position.z - gl_Position.w;
}

