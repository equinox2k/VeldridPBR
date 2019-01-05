#version 100
#ifdef GL_ARB_shading_language_420pack
#extension GL_ARB_shading_language_420pack : require
#endif

struct CameraNear
{
    float uCameraNear;
};

uniform CameraNear _72;

struct CameraFar
{
    float uCameraFar;
};

uniform CameraFar _77;

struct SampleUvOffsetWeights
{
    vec4 uSampleUvOffsetWeights[9];
};

uniform SampleUvOffsetWeights _118;

struct DepthCutOff
{
    float uDepthCutOff;
};

uniform DepthCutOff _173;

struct IsUvOriginTopLeft
{
    int uIsUvOriginTopLeft;
};

uniform IsUvOriginTopLeft _240;

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
    vec4 textureColor = texture2D(SPIRV_Cross_CombinedTextureDepthNormalPointSampler, screenPosition);
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
    float param_1 = _72.uCameraNear;
    float param_2 = _77.uCameraFar;
    return perspectiveDepthToViewZ(param, param_1, param_2);
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
    float weightSum = _118.uSampleUvOffsetWeights[0].z;
    vec4 diffuseSum = texture2D(SPIRV_Cross_CombinedTextureDiffuseLinearSampler, iTexCoord) * weightSum;
    for (int i = 1; i <= 8; i++)
    {
        float sampleWeight = _118.uSampleUvOffsetWeights[i].z;
        vec2 sampleUvOffset = _118.uSampleUvOffsetWeights[i].xy * iInvSize;
        vec2 sampleUv = iTexCoord + sampleUvOffset;
        vec2 param_2 = sampleUv;
        float param_3 = getDepth(param_2);
        float viewZ = -getViewZ(param_3);
        if (abs(viewZ - centerViewZ) > _173.uDepthCutOff)
        {
            rBreak = true;
        }
        if (!rBreak)
        {
            diffuseSum += (texture2D(SPIRV_Cross_CombinedTextureDiffuseLinearSampler, sampleUv) * sampleWeight);
            weightSum += sampleWeight;
        }
        sampleUv = iTexCoord - sampleUvOffset;
        vec2 param_4 = sampleUv;
        float param_5 = getDepth(param_4);
        viewZ = -getViewZ(param_5);
        if (abs(viewZ - centerViewZ) > _173.uDepthCutOff)
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

