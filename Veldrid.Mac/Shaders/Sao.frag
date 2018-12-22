#version 450

#define NUM_SAMPLES 7
#define NUM_RINGS 4

#define PI 3.14159265359
#define PI2 6.28318530718
#define EPSILON 1e-6

layout(set = 1, binding = 0) uniform CameraNear
{
    float uCameraNear;
};

layout(set = 1, binding = 1) uniform CameraFar
{
    float uCameraFar;
};

layout(set = 1, binding = 2) uniform ProjectionMatrix
{
    mat4 uProjectionMatrix;
};

layout(set = 1, binding = 3) uniform InverseProjectionMatrix
{
    mat4 uInverseProjectionMatrix;
};

layout(set = 1, binding = 4) uniform Scale
{
    float uScale;
};

layout(set = 1, binding = 5) uniform Intensity
{
    float uIntensity;
};

layout(set = 1, binding = 6) uniform Bias
{
    float uBias;
};

layout(set = 1, binding = 7) uniform KernelRadius
{
    float uKernelRadius;
};

layout(set = 1, binding = 8) uniform MinResolution
{
    float uMinResolution;
};

layout(set = 1, binding = 9) uniform Size
{
    vec2 uSize;
};

layout(set = 1, binding = 10) uniform Seed
{
    float uSeed;
};

layout(set = 1, binding = 11) uniform texture2D TextureDepthNormal;

layout(set = 1, binding = 12) uniform sampler PointSampler;

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
    return perspectiveDepthToViewZ(depth, uCameraNear, uCameraFar);
}

vec3 getViewPosition(const in vec2 screenPosition, const in float depth, const in float viewZ) {
    float clipW = uProjectionMatrix[2][3] * viewZ + uProjectionMatrix[3][3];
    vec4 clipPosition = vec4((vec3(screenPosition, depth) - 0.5) * 2.0, 1.0);
    clipPosition *= clipW;
    return (uInverseProjectionMatrix * clipPosition).xyz;
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
    vec4 textureColor = texture(sampler2D(TextureDepthNormal, PointSampler), screenPosition);
    return DecodeViewNormalStereo(textureColor);
}

float getDepth(const in vec2 screenPosition) {
    vec4 textureColor = texture(sampler2D(TextureDepthNormal, PointSampler), screenPosition);
    return DecodeFloatRG(textureColor.zw);
}

float scaleDividedByCameraFar;
float minResolutionMultipliedByCameraFar;

float getOcclusion(const in vec3 centerViewPosition, const in vec3 centerViewNormal, const in vec3 sampleViewPosition) {
    vec3 viewDelta = sampleViewPosition - centerViewPosition;
    float viewDistance = length(viewDelta);
    float scaledScreenDistance = scaleDividedByCameraFar * viewDistance;
    return max(0.0, (dot(centerViewNormal, viewDelta) - minResolutionMultipliedByCameraFar) / scaledScreenDistance - uBias) / (1.0 + pow2(scaledScreenDistance));
}

const float ANGLE_STEP = PI2 * float( NUM_RINGS ) / float( NUM_SAMPLES );
const float INV_NUM_SAMPLES = 1.0 / float(NUM_SAMPLES);

float getAmbientOcclusion(const in vec3 centerViewPosition) {
    scaleDividedByCameraFar = uScale / uCameraFar;
    minResolutionMultipliedByCameraFar = uMinResolution * uCameraFar;
    vec3 centerViewNormal = getViewNormal(iTexCoord);
    float angle = rand(iTexCoord + uSeed) * PI2;
    vec2 radius = vec2(uKernelRadius * INV_NUM_SAMPLES) / uSize;
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
    return occlusionSum * (uIntensity / weightSum);
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

