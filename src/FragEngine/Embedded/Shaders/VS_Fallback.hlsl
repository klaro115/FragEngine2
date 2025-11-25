// Constant buffer for per-object data:
cbuffer CBObject : register(b3)
{
    float4x4 mtxWorld;
    float3 minBounds;
    float3 maxBounds;
};

// Basic vertex data:
struct BasicVertex
{
    float3 position         : POSITION;
    float3 normal           : NORMAL0;
    float2 uvs              : TEXCOORD0;
};

// Basic vertex shader output:
struct VertexOutputBasic
{
    float4 position         : SV_Position;
    float3 worldPosition    : POSITION;
    float3 worldNormal      : NORMAL0;
    float2 uvs              : TEXCOORD0;
};

// Vertex shader entry point:
VertexOutputBasic MainVertex(const in BasicVertex _vertexBasic)
{
    const float4 transformedPosition = mul(mtxWorld, float4(_vertexBasic.position, 1));
    const float4 transformedNormal = mul(mtxWorld, float4(_vertexBasic.normal, 0));

    VertexOutputBasic o;
    o.position = transformedPosition;
    o.worldPosition = transformedPosition.xyz;
    o.worldNormal = normalize(transformedNormal.xyz);
    o.uvs = _vertexBasic.uvs;
    return o;
}
