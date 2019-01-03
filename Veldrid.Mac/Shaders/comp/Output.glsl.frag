#version 100
#ifdef GL_ARB_shading_language_420pack
#extension GL_ARB_shading_language_420pack : require
#endif

struct SkyboxDiffuse
{
    vec4 uSkyboxDiffuse;
};

uniform SkyboxDiffuse _56;

struct Opacity
{
    float uOpacity;
};

uniform Opacity _83;

uniform samplerCube SPIRV_Cross_CombinedTextureEnvSkyboxLinearSampler;
uniform sampler2D SPIRV_Cross_CombinedTextureDiffuseLinearSampler;
uniform sampler2D SPIRV_Cross_CombinedTextureAmbientOcclusionLinearSampler;

varying vec3 iViewDirection;
varying vec2 iTexCoord;

void main()
{
    vec4 albedoSkybox = textureCube(SPIRV_Cross_CombinedTextureEnvSkyboxLinearSampler, iViewDirection);
    float rand = fract(sin(dot(iViewDirection.xy, vec2(12.98980045318603515625, 78.233001708984375))) * 43758.546875);
    float dither = mix(-0.00196078442968428134918212890625, 0.00196078442968428134918212890625, rand);
    vec3 _47 = albedoSkybox.xyz + vec3(dither);
    albedoSkybox = vec4(_47.x, _47.y, _47.z, albedoSkybox.w);
    albedoSkybox = max(albedoSkybox, vec4(0.0)) * _56.uSkyboxDiffuse;
    vec4 albedo = texture2D(SPIRV_Cross_CombinedTextureDiffuseLinearSampler, iTexCoord);
    albedo = vec4(albedo.xyz, albedo.w * _83.uOpacity);
    vec4 result = vec4((albedoSkybox.xyz * (1.0 - albedo.w)) + albedo.xyz, albedoSkybox.w);
    vec4 ambientOcclusion = texture2D(SPIRV_Cross_CombinedTextureAmbientOcclusionLinearSampler, vec2(iTexCoord.x, 1.0 - iTexCoord.y));
    gl_FragData[0] = vec4(result * ambientOcclusion);
}

