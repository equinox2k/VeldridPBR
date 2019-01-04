#version 100
#ifdef GL_ARB_shading_language_420pack
#extension GL_ARB_shading_language_420pack : require
#endif

struct CameraNear
{
    float uCameraNear;
};

uniform CameraNear _86;

struct CameraFar
{
    float uCameraFar;
};

uniform CameraFar _90;

struct SampleUvOffsetWeight0
{
    vec4 uSampleUvOffsetWeight0;
};

uniform SampleUvOffsetWeight0 _110;

struct SampleUvOffsetWeight1
{
    vec4 uSampleUvOffsetWeight1;
};

uniform SampleUvOffsetWeight1 _122;

struct SampleUvOffsetWeight2
{
    vec4 uSampleUvOffsetWeight2;
};

uniform SampleUvOffsetWeight2 _133;

struct SampleUvOffsetWeight3
{
    vec4 uSampleUvOffsetWeight3;
};

uniform SampleUvOffsetWeight3 _144;

struct SampleUvOffsetWeight4
{
    vec4 uSampleUvOffsetWeight4;
};

uniform SampleUvOffsetWeight4 _155;

struct SampleUvOffsetWeight5
{
    vec4 uSampleUvOffsetWeight5;
};

uniform SampleUvOffsetWeight5 _166;

struct SampleUvOffsetWeight6
{
    vec4 uSampleUvOffsetWeight6;
};

uniform SampleUvOffsetWeight6 _177;

struct SampleUvOffsetWeight7
{
    vec4 uSampleUvOffsetWeight7;
};

uniform SampleUvOffsetWeight7 _188;

struct SampleUvOffsetWeight8
{
    vec4 uSampleUvOffsetWeight8;
};

uniform SampleUvOffsetWeight8 _199;

struct DepthCutOff
{
    float uDepthCutOff;
};

uniform DepthCutOff _281;

uniform sampler2D SPIRV_Cross_CombinedTextureDepthNormalPointSampler;
uniform sampler2D SPIRV_Cross_CombinedTextureDiffuseLinearSampler;

varying vec2 iTexCoord;
varying vec2 iInvSize;

float DecodeFloatRG(vec2 enc)
{
    vec2 kDecodeDot = vec2(1.0, 0.0039215697906911373138427734375);
    return dot(enc, kDecodeDot);
}

float getDepth(vec2 screenPosition)
{
    vec4 textureColor = texture2D(SPIRV_Cross_CombinedTextureDepthNormalPointSampler, vec2(screenPosition.x, 1.0 - screenPosition.y));
    vec2 param = textureColor.zw;
    return DecodeFloatRG(param);
}

float perspectiveDepthToViewZ(float invClipZ, float near, float far)
{
    return (near * far) / (((far - near) * invClipZ) - far);
}

float getViewZ(float depth)
{
    float param = depth;
    float param_1 = _86.uCameraNear;
    float param_2 = _90.uCameraFar;
    return perspectiveDepthToViewZ(param, param_1, param_2);
}

vec4 getSampleOffsetWeight(int index)
{
    if (index == 0)
    {
        return _110.uSampleUvOffsetWeight0;
    }
    if (index == 1)
    {
        return _122.uSampleUvOffsetWeight1;
    }
    if (index == 2)
    {
        return _133.uSampleUvOffsetWeight2;
    }
    if (index == 3)
    {
        return _144.uSampleUvOffsetWeight3;
    }
    if (index == 4)
    {
        return _155.uSampleUvOffsetWeight4;
    }
    if (index == 5)
    {
        return _166.uSampleUvOffsetWeight5;
    }
    if (index == 6)
    {
        return _177.uSampleUvOffsetWeight6;
    }
    if (index == 7)
    {
        return _188.uSampleUvOffsetWeight7;
    }
    if (index == 8)
    {
        return _199.uSampleUvOffsetWeight8;
    }
    return vec4(0.0);
}

void main()
{
    vec2 param = iTexCoord;
    float depth = getDepth(param);
    if (depth >= 0.999998986721038818359375)
    {
        discard;
    }
    float param_1 = depth;
    float centerViewZ = -getViewZ(param_1);
    bool rBreak = false;
    bool lBreak = false;
    int param_2 = 0;
    float weightSum = getSampleOffsetWeight(param_2).z;
    vec4 diffuseSum = texture2D(SPIRV_Cross_CombinedTextureDiffuseLinearSampler, iTexCoord) * weightSum;
    for (int i = 1; i <= 8; i++)
    {
        int param_3 = i;
        float sampleWeight = getSampleOffsetWeight(param_3).z;
        int param_4 = i;
        vec2 sampleUvOffset = getSampleOffsetWeight(param_4).xy * iInvSize;
        vec2 sampleUv = iTexCoord + sampleUvOffset;
        vec2 param_5 = sampleUv;
        float param_6 = getDepth(param_5);
        float viewZ = -getViewZ(param_6);
        if (abs(viewZ - centerViewZ) > _281.uDepthCutOff)
        {
            rBreak = true;
        }
        if (!rBreak)
        {
            diffuseSum += (texture2D(SPIRV_Cross_CombinedTextureDiffuseLinearSampler, sampleUv) * sampleWeight);
            weightSum += sampleWeight;
        }
        sampleUv = iTexCoord - sampleUvOffset;
        vec2 param_7 = sampleUv;
        float param_8 = getDepth(param_7);
        viewZ = -getViewZ(param_8);
        if (abs(viewZ - centerViewZ) > _281.uDepthCutOff)
        {
            lBreak = true;
        }
        if (!lBreak)
        {
            diffuseSum += (texture2D(SPIRV_Cross_CombinedTextureDiffuseLinearSampler, sampleUv) * sampleWeight);
            weightSum += sampleWeight;
        }
    }
    gl_FragData[0] = diffuseSum / vec4(weightSum);
}

