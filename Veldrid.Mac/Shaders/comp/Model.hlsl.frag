cbuffer ModelMatrix : register(b0)
{
    row_major float4x4 _13_uModelMatrix : packoffset(c0);
};

cbuffer ProjectionMatrix : register(b2)
{
    row_major float4x4 _137_uProjectionMatrix : packoffset(c0);
};

cbuffer ViewMatrix : register(b1)
{
    row_major float4x4 _142_uViewMatrix : packoffset(c0);
};

uniform float4 gl_HalfPixel;

static float4 gl_Position;
static float3 iPosition;
static float3 oPosition;
static float2 oTexCoord;
static float2 iTexCoord;
static float3 iNormal;
static float3 iTangent;
static float3x3 oTBN;
static float3 oNormal;
static float3 oVertexPosition;

struct SPIRV_Cross_Input
{
    float3 iPosition : TEXCOORD0;
    float2 iTexCoord : TEXCOORD1;
    float3 iNormal : TEXCOORD2;
    float3 iTangent : TEXCOORD3;
};

struct SPIRV_Cross_Output
{
    float3 oPosition : TEXCOORD0;
    float2 oTexCoord : TEXCOORD1;
    float3 oNormal : TEXCOORD2;
    float3x3 oTBN : TEXCOORD3;
    float3 oVertexPosition : TEXCOORD6;
    float4 gl_Position : POSITION;
};

void vert_main()
{
    float4 position = mul(float4(iPosition, 1.0f), _13_uModelMatrix);
    oPosition = float3(position.xyz) / position.w.xxx;
    oTexCoord = float2(iTexCoord.x, 1.0f - iTexCoord.y);
    float3 normalW = normalize(float3(mul(float4(iNormal, 0.0f), _13_uModelMatrix).xyz));
    float3 tangentW = normalize(float3(mul(float4(iTangent, 0.0f), _13_uModelMatrix).xyz));
    float3 bitangentW = cross(normalW, tangentW);
    oTBN = float3x3(float3(float3(tangentW)), float3(float3(bitangentW)), float3(float3(normalW)));
    oNormal = normalW;
    oVertexPosition = iPosition;
    gl_Position = mul(position, mul(_142_uViewMatrix, _137_uProjectionMatrix));
    gl_Position.y = -gl_Position.y;
    gl_Position.x = gl_Position.x - gl_HalfPixel.x * gl_Position.w;
    gl_Position.y = gl_Position.y + gl_HalfPixel.y * gl_Position.w;
}

SPIRV_Cross_Output main(SPIRV_Cross_Input stage_input)
{
    iPosition = stage_input.iPosition;
    iTexCoord = stage_input.iTexCoord;
    iNormal = stage_input.iNormal;
    iTangent = stage_input.iTangent;
    vert_main();
    SPIRV_Cross_Output stage_output;
    stage_output.gl_Position = gl_Position;
    stage_output.oPosition = oPosition;
    stage_output.oTexCoord = oTexCoord;
    stage_output.oTBN = oTBN;
    stage_output.oNormal = oNormal;
    stage_output.oVertexPosition = oVertexPosition;
    return stage_output;
}
