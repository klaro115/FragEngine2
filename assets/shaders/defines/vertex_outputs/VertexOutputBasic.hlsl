//<TYP>
// Basic vertex shader output.
struct VertexOutputBasic
{
    float4 position : SV_Position;
    float3 worldPosition : POSITION;
    float3 worldNormal : NORMAL0;
    float2 uvs : TEXCOORD0;
};
//</TYP>
