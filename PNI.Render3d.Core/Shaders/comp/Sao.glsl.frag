#version 100
#ifdef GL_ARB_shading_language_420pack
#extension GL_ARB_shading_language_420pack : require
#endif

struct CameraNear
{
    float uCameraNear;
};

uniform CameraNear _98;

struct CameraFar
{
    float uCameraFar;
};

uniform CameraFar _103;

struct ProjectionMatrix
{
    mat4 uProjectionMatrix;
};

uniform ProjectionMatrix _120;

struct InverseProjectionMatrix
{
    mat4 uInverseProjectionMatrix;
};

uniform InverseProjectionMatrix _153;

struct Bias
{
    float uBias;
};

uniform Bias _260;

struct Scale
{
    float uScale;
};

uniform Scale _274;

struct MinResolution
{
    float uMinResolution;
};

uniform MinResolution _282;

struct Seed
{
    float uSeed;
};

uniform Seed _298;

struct KernelRadius
{
    float uKernelRadius;
};

uniform KernelRadius _310;

struct Size
{
    vec2 uSize;
};

uniform Size _318;

struct Intensity
{
    float uIntensity;
};

uniform Intensity _398;

struct IsUvOriginTopLeft
{
    int uIsUvOriginTopLeft;
};

uniform IsUvOriginTopLeft _443;

uniform sampler2D SPIRV_Cross_CombinedTextureDepthNormalPointSampler;

varying vec2 iTexCoord;
float scaleDividedByCameraFar;
float minResolutionMultipliedByCameraFar;

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
    float param_1 = _98.uCameraNear;
    float param_2 = _103.uCameraFar;
    return perspectiveDepthToViewZ(param, param_1, param_2);
}

vec3 getViewPosition(vec2 screenPosition, float depth, float viewZ)
{
    float clipW = (_120.uProjectionMatrix[2].w * viewZ) + _120.uProjectionMatrix[3].w;
    vec4 clipPosition = vec4((vec3(screenPosition, depth) - vec3(0.5)) * 2.0, 1.0);
    clipPosition *= clipW;
    return (_153.uInverseProjectionMatrix * clipPosition).xyz;
}

vec3 DecodeViewNormalStereo(vec4 enc4)
{
    float kScale = 1.777699947357177734375;
    vec3 nn = (enc4.xyz * vec3(2.0 * kScale, 2.0 * kScale, 0.0)) + vec3(-kScale, -kScale, 1.0);
    float g = 2.0 / dot(nn, nn);
    vec2 _197 = nn.xy * g;
    vec3 n;
    n = vec3(_197.x, _197.y, n.z);
    n.z = g - 1.0;
    return n;
}

vec3 getViewNormal(vec2 screenPosition)
{
    vec4 textureColor = texture2D(SPIRV_Cross_CombinedTextureDepthNormalPointSampler, screenPosition);
    vec4 param = textureColor;
    return DecodeViewNormalStereo(param);
}

float rand(vec2 uv)
{
    float dt = dot(uv, vec2(12.98980045318603515625, 78.233001708984375));
    float sn = mod(dt, 3.1415927410125732421875);
    return fract(sin(sn) * 43758.546875);
}

float pow2(float x)
{
    return x * x;
}

float getOcclusion(vec3 centerViewPosition, vec3 centerViewNormal, vec3 sampleViewPosition)
{
    vec3 viewDelta = sampleViewPosition - centerViewPosition;
    float viewDistance = length(viewDelta);
    float scaledScreenDistance = scaleDividedByCameraFar * viewDistance;
    float param = scaledScreenDistance;
    return max(0.0, ((dot(centerViewNormal, viewDelta) - minResolutionMultipliedByCameraFar) / scaledScreenDistance) - _260.uBias) / (1.0 + pow2(param));
}

float getAmbientOcclusion(vec3 centerViewPosition)
{
    scaleDividedByCameraFar = _274.uScale / _103.uCameraFar;
    minResolutionMultipliedByCameraFar = _282.uMinResolution * _103.uCameraFar;
    vec2 param = iTexCoord;
    vec3 centerViewNormal = getViewNormal(param);
    vec2 param_1 = iTexCoord + vec2(_298.uSeed);
    float angle = rand(param_1) * 6.283185482025146484375;
    vec2 radius = vec2(_310.uKernelRadius * 0.14285714924335479736328125) / _318.uSize;
    vec2 radiusStep = radius;
    float occlusionSum = 0.0;
    float weightSum = 0.0;
    for (int i = 0; i < 7; i++)
    {
        vec2 sampleUv = iTexCoord + (vec2(cos(angle), sin(angle)) * radius);
        radius += radiusStep;
        angle += 3.590391635894775390625;
        vec2 param_2 = sampleUv;
        float sampleDepth = getDepth(param_2);
        if (sampleDepth >= 0.999998986721038818359375)
        {
            continue;
        }
        float param_3 = sampleDepth;
        float sampleViewZ = getViewZ(param_3);
        vec2 param_4 = sampleUv;
        float param_5 = sampleDepth;
        float param_6 = sampleViewZ;
        vec3 sampleViewPosition = getViewPosition(param_4, param_5, param_6);
        vec3 param_7 = centerViewPosition;
        vec3 param_8 = centerViewNormal;
        vec3 param_9 = sampleViewPosition;
        occlusionSum += getOcclusion(param_7, param_8, param_9);
        weightSum += 1.0;
    }
    if (weightSum == 0.0)
    {
        discard;
    }
    return occlusionSum * (_398.uIntensity / weightSum);
}

void main()
{
    vec2 param = iTexCoord;
    float centerDepth = getDepth(param);
    if (centerDepth >= 0.999998986721038818359375)
    {
        discard;
    }
    float param_1 = centerDepth;
    float centerViewZ = getViewZ(param_1);
    vec2 param_2 = iTexCoord;
    float param_3 = centerDepth;
    float param_4 = centerViewZ;
    vec3 viewPosition = getViewPosition(param_2, param_3, param_4);
    vec3 param_5 = viewPosition;
    float _430 = getAmbientOcclusion(param_5);
    float ambientOcclusion = _430;
    gl_FragData[0] = vec4(vec3(1.0) * (1.0 - ambientOcclusion), 1.0);
}

