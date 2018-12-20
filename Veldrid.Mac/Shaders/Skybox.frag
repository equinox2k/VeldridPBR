#version 450

layout(set = 0, binding = 0) uniform State
{
    vec4 skyboxDiffuse;
} state;

layout(set = 0, binding = 1) uniform textureCube textureEnvSkybox;
layout(set = 0, binding = 2) uniform sampler textureEnvSkyboxSampler;

layout(location = 0) in vec3 iViewDirection;

layout(location = 0) out vec4 oFragColor;

void main() 
{
    vec4 albedo = texture(samplerCube(textureEnvSkybox, textureEnvSkyboxSampler), iViewDirection);
    float rand = fract(sin(dot(iViewDirection.xy, vec2(12.9898, 78.233))) * 43758.5453);
    float dither = mix(-0.5 / 255.0, 0.5 / 255.0, rand);         
    albedo.rgb += dither;
    oFragColor = max(albedo, 0.0) * state.skyboxDiffuse;  
}