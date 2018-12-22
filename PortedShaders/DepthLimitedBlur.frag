#version 450

#define KERNEL_RADIUS 8
#define EPSILON 1e-6

layout(set = 0, binding = 0) uniform state
{
    float cameraNear;
    float cameraFar;
    float depthCutOff;
    vec2 sampleUvOffsets[KERNEL_RADIUS + 1];
    float sampleWeights[KERNEL_RADIUS + 1];
};

layout(set = 0, binding = 1) uniform texture2D textureDiffuse;
layout(set = 0, binding = 2) uniform sampler textureDiffuseSampler;
layout(set = 0, binding = 3) uniform texture2D textureDepthNormal;
layout(set = 0, binding = 4) uniform sampler textureDepthNormalSampler;

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

float getDepth(const in vec2 screenPosition) {
    vec4 textureColor = texture(sampler2D(textureDepthNormal, textureDepthNormalSampler), screenPosition);
    return DecodeFloatRG(textureColor.zw);
}

float perspectiveDepthToViewZ(const in float invClipZ, const in float near, const in float far) {
    return (near * far) / ((far - near) * invClipZ - far);
}

float getViewZ(const in float depth) {
    return perspectiveDepthToViewZ(depth, state.cameraNear, state.cameraFar);
}

void main() 
{
    float depth = getDepth(iTexCoord);
    if( depth >= (1.0 - EPSILON)) {
        discard;
    }

    float centerViewZ = -getViewZ(depth);
    bool rBreak = false, lBreak = false;
    float weightSum = state.sampleWeights[0];
    vec4 diffuseSum = texture(sampler2D(textureDiffuse, textureDiffuseSampler), iTexCoord) * weightSum;
    for( int i = 1; i <= KERNEL_RADIUS; i ++ ) {
        float sampleWeight = state.sampleWeights[i];
        vec2 sampleUvOffset = state.sampleUvOffsets[i] * iInvSize;
        vec2 sampleUv = iTexCoord + sampleUvOffset;
        float viewZ = -getViewZ(getDepth(sampleUv));
        if(abs(viewZ - centerViewZ) > state.depthCutOff) rBreak = true;
        if(!rBreak) {
            diffuseSum += texture(sampler2D(textureDiffuse, textureDiffuseSampler), sampleUv) * sampleWeight;
            weightSum += sampleWeight;
        }
        sampleUv = iTexCoord - sampleUvOffset;
        viewZ = -getViewZ( getDepth(sampleUv));
        if (abs(viewZ - centerViewZ) > state.depthCutOff) lBreak = true;
        if (!lBreak) {
            diffuseSum += texture(sampler2D(textureDiffuse, textureDiffuseSampler), sampleUv) * sampleWeight;
            weightSum += sampleWeight;
        }

    }
    oFragColor = diffuseSum / weightSum;
}
