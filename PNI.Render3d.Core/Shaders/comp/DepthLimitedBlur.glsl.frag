#version 100
#ifdef GL_ARB_shading_language_420pack
#extension GL_ARB_shading_language_420pack : require
#endif

struct CameraNear
{
    float uCameraNear;
};

uniform CameraNear _80;

struct CameraFar
{
    float uCameraFar;
};

uniform CameraFar _85;

struct SampleWeights
{
    float uSampleWeights[9];
};

uniform SampleWeights _125;

struct SampleUvOffsets
{
    vec2 uSampleUvOffsets[9];
};

uniform SampleUvOffsets _157;

struct DepthCutOff
{
    float uDepthCutOff;
};

uniform DepthCutOff _182;

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
    float param_1 = _80.uCameraNear;
    float param_2 = _85.uCameraFar;
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
    float weightSum = _125.uSampleWeights[0];
    vec4 diffuseSum = texture2D(SPIRV_Cross_CombinedTextureDiffuseLinearSampler, iTexCoord) * weightSum;
    for (int i = 1; i <= 8; i++)
    {
        float sampleWeight = _125.uSampleWeights[i];
        vec2 sampleUvOffset = _157.uSampleUvOffsets[i] * iInvSize;
        vec2 sampleUv = iTexCoord + sampleUvOffset;
        vec2 param_2 = sampleUv;
        float param_3 = getDepth(param_2);
        float viewZ = -getViewZ(param_3);
        if (abs(viewZ - centerViewZ) > _182.uDepthCutOff)
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
        if (abs(viewZ - centerViewZ) > _182.uDepthCutOff)
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

