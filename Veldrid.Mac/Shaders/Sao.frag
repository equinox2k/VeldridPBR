#version 450

#define NUM_SAMPLES 7
#define NUM_RINGS 4

#define PI 3.14159265359
#define PI2 6.28318530718
#define EPSILON 1e-6

layout(set = 0, binding = 0) uniform State
{
    float cameraNear;
    float cameraFar;
    mat4 projectionMatrix;
    mat4 inverseProjectionMatrix;
    float scale;
    float intensity;
    float bias;
    float kernelRadius;
    float minResolution;
    vec2 size;
    float seed;
} state;

layout(set = 0, binding = 1) uniform texture2D textureDepthNormal;
layout(set = 0, binding = 2) uniform sampler textureDepthNormalSampler;

layout(location = 0) in vec2 iTexCoord;

layout(location = 0) out vec4 oFragColor;

float pow2(const in float x) {
    return x*x;
}

float rand(const in vec2 uv) {
    const float a = 12.9898, b = 78.233, c = 43758.5453;
    float dt = dot(uv.xy, vec2(a, b)), sn = mod(dt, PI);
    return fract(sin(sn) * c);
}

float perspectiveDepthToViewZ(const in float invClipZ, const in float near, const in float far) {
    return (near * far) / ( (far - near) * invClipZ - far);
}

float getViewZ(const in float depth) {
    return perspectiveDepthToViewZ(depth, state.cameraNear, state.cameraFar);
}

vec3 getViewPosition(const in vec2 screenPosition, const in float depth, const in float viewZ) {
    float clipW = state.projectionMatrix[2][3] * viewZ + state.projectionMatrix[3][3];
    vec4 clipPosition = vec4((vec3(screenPosition, depth) - 0.5) * 2.0, 1.0);
    clipPosition *= clipW;
    return (state.inverseProjectionMatrix * clipPosition).xyz;
}

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

vec3 getViewNormal(const in vec2 screenPosition) {
    vec4 textureColor = texture(sampler2D(textureDepthNormal, textureDepthNormalSampler), screenPosition);
    return DecodeViewNormalStereo(textureColor);
}

float getDepth(const in vec2 screenPosition) {
    vec4 textureColor = texture(sampler2D(textureDepthNormal, textureDepthNormalSampler), screenPosition);
    return DecodeFloatRG(textureColor.zw);
}

float scaleDividedByCameraFar;
float minResolutionMultipliedByCameraFar;

float getOcclusion(const in vec3 centerViewPosition, const in vec3 centerViewNormal, const in vec3 sampleViewPosition) {
    vec3 viewDelta = sampleViewPosition - centerViewPosition;
    float viewDistance = length(viewDelta);
    float scaledScreenDistance = scaleDividedByCameraFar * viewDistance;
    return max(0.0, (dot(centerViewNormal, viewDelta) - minResolutionMultipliedByCameraFar) / scaledScreenDistance - state.bias) / (1.0 + pow2(scaledScreenDistance));
}

const float ANGLE_STEP = PI2 * float( NUM_RINGS ) / float( NUM_SAMPLES );
const float INV_NUM_SAMPLES = 1.0 / float(NUM_SAMPLES);

float getAmbientOcclusion(const in vec3 centerViewPosition) {
    scaleDividedByCameraFar = state.scale / state.cameraFar;
    minResolutionMultipliedByCameraFar = state.minResolution * state.cameraFar;
    vec3 centerViewNormal = getViewNormal(iTexCoord);
    float angle = rand(iTexCoord + state.seed) * PI2;
    vec2 radius = vec2(state.kernelRadius * INV_NUM_SAMPLES) / state.size;
    vec2 radiusStep = radius;
    float occlusionSum = 0.0;
    float weightSum = 0.0;
    for (int i = 0; i < NUM_SAMPLES; i ++) {
        vec2 sampleUv = iTexCoord + vec2(cos(angle), sin(angle)) * radius;
        radius += radiusStep;
        angle += ANGLE_STEP;
        float sampleDepth = getDepth(sampleUv);
        if (sampleDepth >= (1.0 - EPSILON)) {
            continue;
        }
        float sampleViewZ = getViewZ(sampleDepth);
        vec3 sampleViewPosition = getViewPosition(sampleUv, sampleDepth, sampleViewZ);
        occlusionSum += getOcclusion(centerViewPosition, centerViewNormal, sampleViewPosition);
        weightSum += 1.0;
    }
    if (weightSum == 0.0) discard;
    return occlusionSum * (state.intensity / weightSum);
}

void main() {
    float centerDepth = getDepth(iTexCoord);
    if (centerDepth >= (1.0 - EPSILON)) {
        discard;
    }
    float centerViewZ = getViewZ(centerDepth);
    vec3 viewPosition = getViewPosition(iTexCoord, centerDepth, centerViewZ);
    float ambientOcclusion = getAmbientOcclusion(viewPosition);
    oFragColor = vec4(vec3(1.0) * (1.0 - ambientOcclusion), 1.0);
}

