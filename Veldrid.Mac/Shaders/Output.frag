#version 450

layout(set = 0, binding = 0) uniform State
{
    float opacity;
} state;

layout(set = 0, binding = 1) uniform texture2D textureDiffuse;
layout(set = 0, binding = 2) uniform sampler textureDiffuseSampler;
layout(set = 0, binding = 3) uniform texture2D textureAmbientOcclusion;
layout(set = 0, binding = 4) uniform sampler textureAmbientOcclusionSampler;

layout(location = 0) in vec2 iTexCoord;

layout(location = 0) out vec4 oFragColor;

void main() 
{
    vec4 albedo = texture(sampler2D(textureDiffuse, textureDiffuseSampler), iTexCoord.st);
    vec3 ambientOcclusion = texture(sampler2D(textureAmbientOcclusion, textureAmbientOcclusionSampler), iTexCoord.st).rgb;
    oFragColor = vec4(albedo.rgb * ambientOcclusion, albedo.a * state.opacity);
}
