#version 450

layout(location = 0) in vec3 iNormal;

layout(location = 0) out vec4 oFragColor;

vec2 EncodeFloatRG(float v)
{
    vec2 kEncodeMul = vec2(1.0, 255.0);
    float kEncodeBit = 0.00392157;
    vec2 enc = kEncodeMul * v;
    enc = fract (enc);
    enc.x -= enc.y * kEncodeBit;
    return enc;
}

vec2 EncodeViewNormalStereo(vec3 n)
{
    float kScale = 1.7777;
    vec2 enc = n.xy / (n.z + 1.0);
    enc /= kScale;
    enc = enc * 0.5 + 0.5;
    return enc;
}

vec4 EncodeDepthNormal(float depth, vec3 normal)
{
    vec4 enc;
    enc.xy = EncodeViewNormalStereo(normal);
    enc.zw = EncodeFloatRG(depth);
    return enc;
}

void main() 
{
    oFragColor = EncodeDepthNormal(gl_FragCoord.z, iNormal);
}
