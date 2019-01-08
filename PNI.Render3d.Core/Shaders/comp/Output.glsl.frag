#version 100
#ifdef GL_ARB_shading_language_420pack
#extension GL_ARB_shading_language_420pack : require
#endif

struct IsUvOriginTopLeft
{
    int uIsUvOriginTopLeft;
};

uniform IsUvOriginTopLeft _9;

struct SkyboxDiffuse
{
    vec4 uSkyboxDiffuse;
};

uniform SkyboxDiffuse _85;

struct Opacity
{
    float uOpacity;
};

uniform Opacity _107;

uniform samplerCube SPIRV_Cross_CombinedTextureEnvSkyboxLinearSampler;
uniform sampler2D SPIRV_Cross_CombinedTextureDiffuseLinearSampler;

varying vec2 iTexCoord;
varying vec3 iViewDirection;

void main()
{
    vec2 texCoord;
    if (_9.uIsUvOriginTopLeft == 0)
    {
        texCoord = vec2(iTexCoord.x, 1.0 - iTexCoord.y);
    }
    else
    {
        texCoord = iTexCoord;
    }
    vec4 albedoSkybox = textureCube(SPIRV_Cross_CombinedTextureEnvSkyboxLinearSampler, iViewDirection);
    float rand = fract(sin(dot(iViewDirection.xy, vec2(12.98980045318603515625, 78.233001708984375))) * 43758.546875);
    float dither = mix(-0.00196078442968428134918212890625, 0.00196078442968428134918212890625, rand);
    vec3 _76 = albedoSkybox.xyz + vec3(dither);
    albedoSkybox = vec4(_76.x, _76.y, _76.z, albedoSkybox.w);
    albedoSkybox = max(albedoSkybox, vec4(0.0)) * _85.uSkyboxDiffuse;
    vec4 albedo = texture2D(SPIRV_Cross_CombinedTextureDiffuseLinearSampler, texCoord);
    albedo = vec4(albedo.xyz, albedo.w * _107.uOpacity);
    gl_FragData[0] = vec4((albedoSkybox.xyz * (1.0 - albedo.w)) + albedo.xyz, albedoSkybox.w);
}

