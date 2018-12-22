#version 450

layout(set = 1, binding = 0) uniform Opacity
{
    float uOpacity;
};

layout(set = 1, binding = 1) uniform texture2D TextureDiffuse;

layout(set = 1, binding = 2) uniform texture2D TextureAmbientOcclusion;

layout(set = 1, binding = 3) uniform sampler LinearSampler;

layout(location = 0) in vec2 iTexCoord;

layout(location = 0) out vec4 oFragColor;

void main() 
{
    vec4 albedo = texture(sampler2D(TextureDiffuse, LinearSampler), iTexCoord.st);
    vec3 ambientOcclusion = texture(sampler2D(TextureAmbientOcclusion, LinearSampler), iTexCoord.st).rgb;
    oFragColor = vec4(albedo.rgb * ambientOcclusion, albedo.a * uOpacity);
}
