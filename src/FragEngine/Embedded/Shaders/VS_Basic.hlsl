// Basic vertex data:
struct BasicVertex
{
    float3 position : POSITION;
    float3 normal : NORMAL0;
    float2 uvs : TEXCOORD0;
};

// Basic vertex shader output:
struct VertexOutputBasic
{
    float4 position : SV_Position;
    float3 worldPosition : POSITION;
    float3 worldNormal : NORMAL0;
    float2 uvs : TEXCOORD0;
};

// Vertex shader entry point:
VertexOutputBasic MainVertex(const in BasicVertex _vertexBasic)
{
    VertexOutputBasic o;
    o.position = float4(_vertexBasic.position, 0);
    o.worldPosition = _vertexBasic.position.xyz;
    o.worldNormal = normalize(_vertexBasic.normal.xyz);
    o.uvs = _vertexBasic.uvs;
    return o;
}
