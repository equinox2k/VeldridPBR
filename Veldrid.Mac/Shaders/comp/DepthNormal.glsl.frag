#version 100
#ifdef GL_ARB_shading_language_420pack
#extension GL_ARB_shading_language_420pack : require
#endif

varying vec3 iNormal;

vec2 EncodeViewNormalStereo(vec3 n)
{
    float kScale = 1.777699947357177734375;
    vec2 enc = n.xy / vec2(n.z + 1.0);
    enc /= vec2(kScale);
    enc = (enc * 0.5) + vec2(0.5);
    return enc;
}

vec2 EncodeFloatRG(float v)
{
    vec2 kEncodeMul = vec2(1.0, 255.0);
    float kEncodeBit = 0.0039215697906911373138427734375;
    vec2 enc = kEncodeMul * v;
    enc = fract(enc);
    enc.x -= (enc.y * kEncodeBit);
    return enc;
}

vec4 EncodeDepthNormal(float depth, vec3 normal)
{
    vec3 param = normal;
    vec2 _79 = EncodeViewNormalStereo(param);
    vec4 enc;
    enc = vec4(_79.x, _79.y, enc.z, enc.w);
    float param_1 = depth;
    vec2 _84 = EncodeFloatRG(param_1);
    enc = vec4(enc.x, enc.y, _84.x, _84.y);
    return enc;
}

void main()
{
    float param = gl_FragCoord.z;
    vec3 param_1 = iNormal;
    gl_FragData[0] = EncodeDepthNormal(param, param_1);
}

