//<INC>
#include "defines/constants/CBObject.hlsl"
#include "defines/vertex_inputs/BasicVertex.hlsl"
#include "defines/vertex_outputs/VertexOutputBasic.hlsl"
//</INC>

//<SHA>
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
//</SHA>
