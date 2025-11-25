//<TYP>
// Constant buffer for per-object data.
cbuffer CBObject : register(b3)
{
    float4x4 mtxWorld;
    float3 minBounds;
    float3 maxBounds;
    //...
};
//</TYP>
