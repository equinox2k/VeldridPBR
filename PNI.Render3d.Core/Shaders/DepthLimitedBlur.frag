#version 450

#define KERNEL_RADIUS 8
#define EPSILON 1e-6

layout(set = 1, binding = 0) uniform IsUvOriginTopLeft
{
    int uIsUvOriginTopLeft;
};

layout(set = 1, binding = 1) uniform CameraNear
{
    float uCameraNear;
};

layout(set = 1, binding = 2) uniform CameraFar
{
    float uCameraFar;
};

layout(set = 1, binding = 3) uniform DepthCutOff
{
    float uDepthCutOff;
};

layout(set = 1, binding = 4) uniform SampleUvOffsetWeights
{
    vec4 uSampleUvOffsetWeights[KERNEL_RADIUS + 1];
};

layout(set = 1, binding = 5) uniform sampler LinearSampler;

layout(set = 1, binding = 6) uniform sampler PointSampler;

layout(set = 2, binding = 0) uniform texture2D TextureDepthNormal;

layout(set = 2, binding = 1) uniform texture2D TextureDiffuse;

layout(location = 0) in vec2 iTexCoord;
layout(location = 1) in vec2 iInvSize;

layout(location = 0) out vec4 oFragColor;

const float UnpackDownscale = 255.0 / 256.0;
const vec3 PackFactors = vec3(256.0 * 256.0 * 256.0, 256.0 * 256.0, 256.0);
const vec4 UnpackFactors = UnpackDownscale / vec4(PackFactors, 1.0);

float DecodeFloatRG(vec2 enc)
{
    vec2 kDecodeDot = vec2(1.0, 0.00392157);
    return dot(enc, kDecodeDot);
}

vec3 DecodeViewNormalStereo(vec4 enc4)
{
    float kScale = 1.7777;
    vec3 nn = enc4.xyz * vec3(2.0 * kScale, 2.0 * kScale, 0.0) + vec3(-kScale, -kScale, 1.0);
    float g = 2.0 / dot(nn.xyz,nn.xyz);
    vec3 n;
    n.xy = g*nn.xy;
    n.z = g - 1.0;
    return n;
}

float getDepth(vec2 screenPosition) {
    vec4 textureColor = texture(sampler2D(TextureDepthNormal, PointSampler), screenPosition);
    return DecodeFloatRG(textureColor.zw);
}

float perspectiveDepthToViewZ(float invClipZ, float near, float far) {
    return (near * far) / ((far - near) * invClipZ - far);
}

float getViewZ(float depth) {
    return perspectiveDepthToViewZ(depth, uCameraNear, uCameraFar);
}

void main() 
{
    float depth = getDepth(iTexCoord);
    if( depth >= (1.0 - EPSILON)) {
        discard;
    }

    float centerViewZ = -getViewZ(depth);
    bool rBreak = false, lBreak = false;
    float weightSum = uSampleUvOffsetWeights[0].z;
    vec4 diffuseSum = texture(sampler2D(TextureDiffuse, LinearSampler), iTexCoord) * weightSum;
    for (int i = 1; i <= KERNEL_RADIUS; i ++ ) {
        float sampleWeight = uSampleUvOffsetWeights[i].z;
        vec2 sampleUvOffset = uSampleUvOffsetWeights[i].xy * iInvSize;
        vec2 sampleUv = iTexCoord + sampleUvOffset;
        float viewZ = -getViewZ(getDepth(sampleUv));
        if(abs(viewZ - centerViewZ) > uDepthCutOff) rBreak = true;
        if(!rBreak) {
            diffuseSum += texture(sampler2D(TextureDiffuse, LinearSampler), sampleUv) * sampleWeight;
            weightSum += sampleWeight;
        }
        sampleUv = iTexCoord - sampleUvOffset;
        viewZ = -getViewZ( getDepth(sampleUv));
        if (abs(viewZ - centerViewZ) > uDepthCutOff) lBreak = true;
        if (!lBreak) {
            diffuseSum += texture(sampler2D(TextureDiffuse, LinearSampler), sampleUv) * sampleWeight;
            weightSum += sampleWeight;
        }
    }
    oFragColor = diffuseSum / weightSum;
}
