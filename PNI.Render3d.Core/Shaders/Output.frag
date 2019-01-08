#version 450

layout(set = 1, binding = 0) uniform IsUvOriginTopLeft
{
    int uIsUvOriginTopLeft;
};

layout(set = 1, binding = 1) uniform Opacity
{
    float uOpacity;
};

layout(set = 1, binding = 2) uniform SkyboxDiffuse
{
    vec4 uSkyboxDiffuse;
};

layout(set = 1, binding = 3) uniform textureCube TextureEnvSkybox;

layout(set = 1, binding = 4) uniform sampler LinearSampler;

layout(set = 2, binding = 0) uniform texture2D TextureDiffuse;

layout(location = 0) in vec2 iTexCoord;
layout(location = 1) in vec3 iViewDirection;

layout(location = 0) out vec4 oFragColor;

void main() 
{
    vec2 texCoord;
    if (uIsUvOriginTopLeft == 0)
    {
        texCoord = vec2(iTexCoord.x, 1.0 - iTexCoord.y);
    }
    else
    {
        texCoord = iTexCoord;
    }

    vec4 albedoSkybox = texture(samplerCube(TextureEnvSkybox, LinearSampler), iViewDirection);
    float rand = fract(sin(dot(iViewDirection.xy, vec2(12.9898, 78.233))) * 43758.5453);
    float dither = mix(-0.5 / 255.0, 0.5 / 255.0, rand);         
    albedoSkybox.rgb += dither;
    albedoSkybox = max(albedoSkybox, 0.0) * uSkyboxDiffuse; 
   
    vec4 albedo = texture(sampler2D(TextureDiffuse, LinearSampler), texCoord);
    albedo = vec4(albedo.rgb, albedo.a * uOpacity);
    
    oFragColor = vec4((albedoSkybox.rgb * (1.0 - albedo.a)) + albedo.rgb, albedoSkybox.a);
}
