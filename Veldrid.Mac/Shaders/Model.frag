#version 450

layout(set = 0, binding = 0) uniform State
{
    vec4 diffuseColor;
    int useTextureDiffuse;
    int useTextureBumpmap;
    int useTextureEffect;
    vec4 effect;
    vec3 lightDirection;
    vec3 lightColor;
    vec2 metallicRoughnessValues;
    vec3 cameraPosition;
} state;

layout(set = 0, binding = 1) uniform textureCube textureEnvMapDiffuse;
layout(set = 0, binding = 2) uniform sampler textureEnvMapDiffuseSampler;
layout(set = 0, binding = 3) uniform textureCube textureEnvMapSpecular;
layout(set = 0, binding = 4) uniform sampler textureEnvMapSpecularSampler;
layout(set = 0, binding = 5) uniform textureCube textureEnvMapGloss;
layout(set = 0, binding = 6) uniform sampler textureEnvMapGlossSampler;
layout(set = 0, binding = 7) uniform texture2D textureBRDF;
layout(set = 0, binding = 8) uniform sampler textureBRDFSampler;
layout(set = 0, binding = 9) uniform texture2D textureDiffuse;
layout(set = 0, binding = 10) uniform sampler textureDiffuseSampler;
layout(set = 0, binding = 11) uniform texture2D textureBumpmap;
layout(set = 0, binding = 12) uniform sampler textureBumpmapSampler;
layout(set = 0, binding = 13) uniform texture2D textureEffect;
layout(set = 0, binding = 14) uniform sampler textureEffectSampler;

layout(location = 0) in vec3 iPosition;
layout(location = 1) in vec2 iTexCoord;
layout(location = 2) in vec3 iNormal;
layout(location = 3) in mat3 iTBN;
layout(location = 6) in vec3 iVertexPosition;

layout(location = 0) out vec4 oFragColor;

struct PBRInfo {
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

const float cPI = 3.141592653589793;
const float cMinRoughness = 0.04;

vec3 getNormal()
{
    if (state.useTextureBumpmap == 0)
    {
        return normalize(iTBN[2].xyz);
    } 
    else 
    {
        vec3 n = texture(sampler2D(textureBumpmap, textureBumpmapSampler), iTexCoord).rgb;
        return normalize(iTBN * (2.0 * n - 1.0));
    }
}

vec3 getIBLContribution(PBRInfo pbrInputs, vec3 n, vec3 reflection, bool gloss)
{
    vec3 brdf = texture(sampler2D(textureBRDF, textureBRDFSampler), vec2(pbrInputs.NdotV, 1.0 - pbrInputs.perceptualRoughness)).rgb;
    vec3 diffuseLight = texture(samplerCube(textureEnvMapDiffuse, textureEnvMapDiffuseSampler), n).rgb;
    vec3 specularLight;
    if (gloss == true)
    {
        specularLight = texture(samplerCube(textureEnvMapGloss, textureEnvMapGlossSampler), reflection).rgb;
    }
    else
    {
        specularLight = texture(samplerCube(textureEnvMapSpecular,textureEnvMapSpecularSampler), reflection).rgb;
    }
    specularLight = mix(pbrInputs.diffuseColor, specularLight, pbrInputs.metalness);
    vec3 diffuse = diffuseLight * pbrInputs.diffuseColor;
    vec3 specular = specularLight * (pbrInputs.specularColor * brdf.x + brdf.y);
    return diffuse + specular;
}

vec3 diffuse(PBRInfo pbrInputs)
{
    return pbrInputs.diffuseColor / cPI;
}

vec3 specularReflection(PBRInfo pbrInputs)
{
    return pbrInputs.metalness + (vec3(1.0) - pbrInputs.metalness) * pow(1.0 - pbrInputs.VdotH, 5.0);
}

float geometricOcclusion(PBRInfo pbrInputs)
{
    float NdotL = pbrInputs.NdotL;
    float NdotV = pbrInputs.NdotV;
    float r = pbrInputs.alphaRoughness;
    float attenuationL = 2.0 * NdotL / (NdotL + sqrt(r * r + (1.0 - r * r) * (NdotL * NdotL)));
    float attenuationV = 2.0 * NdotV / (NdotV + sqrt(r * r + (1.0 - r * r) * (NdotV * NdotV)));
    return attenuationL * attenuationV;
}

float microfacetDistribution(PBRInfo pbrInputs)
{
    float roughnessSq = pbrInputs.alphaRoughness * pbrInputs.alphaRoughness;
    float f = (pbrInputs.NdotH * roughnessSq - pbrInputs.NdotH) * pbrInputs.NdotH + 1.0;
    return roughnessSq / (cPI * f * f);
}

void main()
{
    float metalValue = state.metallicRoughnessValues.x;
    float roughValue = state.metallicRoughnessValues.y;
    bool glossy = false;

    if (state.useTextureEffect != 0)
    {
        vec4 effectColor = texture(sampler2D(textureEffect, textureEffectSampler), iTexCoord);
        if (effectColor.r >= 0.1)
        {
            metalValue = 1.0;
            roughValue = 0.5;
            glossy = true;
        }
    }

    float perceptualRoughness = clamp(roughValue, cMinRoughness, 1.0);
    float metallic = clamp(metalValue, 0.0, 1.0);
    float alphaRoughness = perceptualRoughness * perceptualRoughness;

    vec4 baseColor = state.diffuseColor;
    if (state.useTextureDiffuse != 0)
    {
        baseColor = texture(sampler2D(textureDiffuse, textureDiffuseSampler), iTexCoord);
    }

    if (int(state.effect.x) == 1) 
    {
        vec3 lumCoeff = vec3(0.25, 0.65, 0.1);
        float lum = dot(lumCoeff, baseColor.rgb);
        vec3 blend = vec3( lum );
        float L = min(1.0, max(0.0, 10.0 * (lum - 0.45)));
        vec3 result1 = 2.0 * baseColor.rgb * blend;
        vec3 result2 = 1.0 - 2.0 * (1.0 - blend) * (1.0 - baseColor.rgb);
        baseColor = vec4((vec3(1.0) + mix(result1, result2, L)) / 2.0, baseColor.a);
    }

    if (int(state.effect.x) == 2) 
    {
        float minY = state.effect.z;
        float maxY = state.effect.w;
        float effectValue = ((1.0 - ((iVertexPosition.y - minY) / (maxY - minY))) - 2.0) + (state.effect.y * 4.0);
        baseColor = mix(vec4(0.0, 0.0, 0.0, 1.0), baseColor, max(0.0, min(1.0, effectValue)));
    }

    vec3 specularColor = mix(vec3(0.04), baseColor.rgb, metallic);

    float reflectance = max(max(specularColor.r, specularColor.g), specularColor.b);
    float reflectance90 = clamp(reflectance * 25.0, 0.0, 1.0);
    vec3 specularEnvironmentR0 = specularColor.rgb;
    vec3 specularEnvironmentR90 = vec3(1.0, 1.0, 1.0) * reflectance90;
    vec3 n = getNormal();
    vec3 v = normalize(state.cameraPosition - iPosition);
    vec3 l = normalize(state.lightDirection);
    vec3 h = normalize(l+v);
    vec3 reflection = -normalize(reflect(v, n));
    float NdotL = clamp(dot(n, l), 0.001, 1.0);
    float NdotV = abs(dot(n, v)) + 0.001;
    float NdotH = clamp(dot(n, h), 0.0, 1.0);
    float LdotH = clamp(dot(l, h), 0.0, 1.0);
    float VdotH = clamp(dot(v, h), 0.0, 1.0);

    PBRInfo pbrInputs = PBRInfo(NdotL, NdotV, NdotH, LdotH, VdotH, perceptualRoughness, metallic, specularEnvironmentR0, specularEnvironmentR90, alphaRoughness, baseColor.rgb, specularColor);

    vec3 F = specularReflection(pbrInputs);
    float G = geometricOcclusion(pbrInputs);
    float D = microfacetDistribution(pbrInputs);
    vec3 diffuseContrib = (1.0 - F) * diffuse(pbrInputs);
    vec3 specContrib = F * G * D / (4.0 * NdotL * NdotV);
    vec3 color = NdotL * state.lightColor * (diffuseContrib + specContrib);
    color += getIBLContribution(pbrInputs, n, reflection, glossy);

    oFragColor = vec4(color, baseColor.a);
}