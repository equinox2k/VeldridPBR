#version 100
#ifdef GL_ARB_shading_language_420pack
#extension GL_ARB_shading_language_420pack : require
#endif

struct PBRInfo
{
    float NdotL;
    float NdotV;
    float NdotH;
    float LdotH;
    float VdotH;
    float perceptualRoughness;
    float metalness;
    vec3 reflectance0;
    vec3 reflectance90;
    float alphaRoughness;
    vec3 diffuseColor;
    vec3 specularColor;
};

struct UseTextureBumpmap
{
    int uUseTextureBumpmap;
};

uniform UseTextureBumpmap _43;

struct MetallicRoughnessValues
{
    vec2 uMetallicRoughnessValues;
};

uniform MetallicRoughnessValues _288;

struct UseTextureEffect
{
    int uUseTextureEffect;
};

uniform UseTextureEffect _299;

struct DiffuseColor
{
    vec4 uDiffuseColor;
};

uniform DiffuseColor _335;

struct UseTextureDiffuse
{
    int uUseTextureDiffuse;
};

uniform UseTextureDiffuse _341;

struct Effect
{
    vec4 uEffect;
};

uniform Effect _355;

struct CameraPosition
{
    vec3 uCameraPosition;
};

uniform CameraPosition _485;

struct LightDirection
{
    vec3 uLightDirection;
};

uniform LightDirection _496;

struct LightColor
{
    vec3 uLightColor;
};

uniform LightColor _589;

struct IsUvOriginTopLeft
{
    int uIsUvOriginTopLeft;
};

uniform IsUvOriginTopLeft _619;

uniform sampler2D SPIRV_Cross_CombinedTextureEffectPointSampler;
uniform sampler2D SPIRV_Cross_CombinedTextureDiffuseLinearSampler;
uniform sampler2D SPIRV_Cross_CombinedTextureBumpmapLinearSampler;
uniform sampler2D SPIRV_Cross_CombinedTextureBRDFPointSampler;
uniform samplerCube SPIRV_Cross_CombinedTextureEnvMapDiffuseLinearSampler;
uniform samplerCube SPIRV_Cross_CombinedTextureEnvMapGlossLinearSampler;
uniform samplerCube SPIRV_Cross_CombinedTextureEnvMapSpecularLinearSampler;

varying mat3 iTBN;
varying vec2 iTexCoord;
varying vec3 iVertexPosition;
varying vec3 iPosition;
varying vec3 iNormal;

vec3 _86;

vec3 getNormal(vec2 texCoord)
{
    if (_43.uUseTextureBumpmap == 0)
    {
        return normalize(iTBN[2]);
    }
    else
    {
        vec3 n = texture2D(SPIRV_Cross_CombinedTextureBumpmapLinearSampler, texCoord).xyz;
        return normalize(iTBN * ((n * 2.0) - vec3(1.0)));
    }
}

vec3 specularReflection(PBRInfo pbrInputs)
{
    return vec3(pbrInputs.metalness) + ((vec3(1.0) - vec3(pbrInputs.metalness)) * pow(1.0 - pbrInputs.VdotH, 5.0));
}

float geometricOcclusion(PBRInfo pbrInputs)
{
    float NdotL = pbrInputs.NdotL;
    float NdotV = pbrInputs.NdotV;
    float r = pbrInputs.alphaRoughness;
    float attenuationL = (2.0 * NdotL) / (NdotL + sqrt((r * r) + ((1.0 - (r * r)) * (NdotL * NdotL))));
    float attenuationV = (2.0 * NdotV) / (NdotV + sqrt((r * r) + ((1.0 - (r * r)) * (NdotV * NdotV))));
    return attenuationL * attenuationV;
}

float microfacetDistribution(PBRInfo pbrInputs)
{
    float roughnessSq = pbrInputs.alphaRoughness * pbrInputs.alphaRoughness;
    float f = (((pbrInputs.NdotH * roughnessSq) - pbrInputs.NdotH) * pbrInputs.NdotH) + 1.0;
    return roughnessSq / ((3.1415927410125732421875 * f) * f);
}

vec3 diffuse(PBRInfo pbrInputs)
{
    return pbrInputs.diffuseColor / vec3(3.1415927410125732421875);
}

vec3 getIBLContribution(PBRInfo pbrInputs, vec3 n, vec3 reflection, bool gloss)
{
    vec3 brdf = texture2D(SPIRV_Cross_CombinedTextureBRDFPointSampler, vec2(pbrInputs.NdotV, 1.0 - pbrInputs.perceptualRoughness)).xyz;
    vec3 diffuseLight = textureCube(SPIRV_Cross_CombinedTextureEnvMapDiffuseLinearSampler, n).xyz;
    vec3 specularLight;
    if (gloss == true)
    {
        specularLight = textureCube(SPIRV_Cross_CombinedTextureEnvMapGlossLinearSampler, reflection).xyz;
    }
    else
    {
        specularLight = textureCube(SPIRV_Cross_CombinedTextureEnvMapSpecularLinearSampler, reflection).xyz;
    }
    specularLight = mix(pbrInputs.diffuseColor, specularLight, vec3(pbrInputs.metalness));
    vec3 diffuse_1 = diffuseLight * pbrInputs.diffuseColor;
    vec3 specular = specularLight * ((pbrInputs.specularColor * brdf.x) + vec3(brdf.y));
    return diffuse_1 + specular;
}

void main()
{
    vec2 texCoord = vec2(iTexCoord.x, 1.0 - iTexCoord.y);
    float metalValue = _288.uMetallicRoughnessValues.x;
    float roughValue = _288.uMetallicRoughnessValues.y;
    bool glossy = false;
    if (_299.uUseTextureEffect != 0)
    {
        vec4 effectColor = texture2D(SPIRV_Cross_CombinedTextureEffectPointSampler, texCoord);
        if (effectColor.x >= 0.100000001490116119384765625)
        {
            metalValue = 1.0;
            roughValue = 0.5;
            glossy = true;
        }
    }
    float perceptualRoughness = clamp(roughValue, 0.039999999105930328369140625, 1.0);
    float metallic = clamp(metalValue, 0.0, 1.0);
    float alphaRoughness = perceptualRoughness * perceptualRoughness;
    vec4 baseColor = _335.uDiffuseColor;
    if (_341.uUseTextureDiffuse != 0)
    {
        baseColor = texture2D(SPIRV_Cross_CombinedTextureDiffuseLinearSampler, texCoord);
    }
    if (int(_355.uEffect.x) == 1)
    {
        vec3 lumCoeff = vec3(0.25, 0.64999997615814208984375, 0.100000001490116119384765625);
        float lum = dot(lumCoeff, baseColor.xyz);
        vec3 blend = vec3(lum);
        float L = min(1.0, max(0.0, 10.0 * (lum - 0.449999988079071044921875)));
        vec3 result1 = (baseColor.xyz * 2.0) * blend;
        vec3 result2 = vec3(1.0) - (((vec3(1.0) - blend) * 2.0) * (vec3(1.0) - baseColor.xyz));
        baseColor = vec4((vec3(1.0) + mix(result1, result2, vec3(L))) / vec3(2.0), baseColor.w);
    }
    if (int(_355.uEffect.x) == 2)
    {
        float minY = _355.uEffect.z;
        float maxY = _355.uEffect.w;
        float effectValue = ((1.0 - ((iVertexPosition.y - minY) / (maxY - minY))) - 2.0) + (_355.uEffect.y * 4.0);
        baseColor = mix(vec4(0.0, 0.0, 0.0, 1.0), baseColor, vec4(max(0.0, min(1.0, effectValue))));
    }
    vec3 specularColor = mix(vec3(0.039999999105930328369140625), baseColor.xyz, vec3(metallic));
    float reflectance = max(max(specularColor.x, specularColor.y), specularColor.z);
    float reflectance90 = clamp(reflectance * 25.0, 0.0, 1.0);
    vec3 specularEnvironmentR0 = specularColor;
    vec3 specularEnvironmentR90 = vec3(1.0) * reflectance90;
    vec2 param = texCoord;
    vec3 n = getNormal(param);
    vec3 v = normalize(_485.uCameraPosition - iPosition);
    vec3 l = normalize(_496.uLightDirection);
    vec3 h = normalize(l + v);
    vec3 reflection = -normalize(reflect(v, n));
    float NdotL = clamp(dot(n, l), 0.001000000047497451305389404296875, 1.0);
    float NdotV = abs(dot(n, v)) + 0.001000000047497451305389404296875;
    float NdotH = clamp(dot(n, h), 0.0, 1.0);
    float LdotH = clamp(dot(l, h), 0.0, 1.0);
    float VdotH = clamp(dot(v, h), 0.0, 1.0);
    PBRInfo pbrInputs = PBRInfo(NdotL, NdotV, NdotH, LdotH, VdotH, perceptualRoughness, metallic, specularEnvironmentR0, specularEnvironmentR90, alphaRoughness, baseColor.xyz, specularColor);
    PBRInfo param_1 = pbrInputs;
    vec3 F = specularReflection(param_1);
    PBRInfo param_2 = pbrInputs;
    float G = geometricOcclusion(param_2);
    PBRInfo param_3 = pbrInputs;
    float D = microfacetDistribution(param_3);
    PBRInfo param_4 = pbrInputs;
    vec3 diffuseContrib = (vec3(1.0) - F) * diffuse(param_4);
    vec3 specContrib = ((F * G) * D) / vec3((4.0 * NdotL) * NdotV);
    vec3 color = (_589.uLightColor * NdotL) * (diffuseContrib + specContrib);
    PBRInfo param_5 = pbrInputs;
    vec3 param_6 = n;
    vec3 param_7 = reflection;
    bool param_8 = glossy;
    color += getIBLContribution(param_5, param_6, param_7, param_8);
    gl_FragData[0] = vec4(color, baseColor.w);
}

