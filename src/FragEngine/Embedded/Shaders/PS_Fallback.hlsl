// Basic vertex shader output:
struct VertexOutputBasic
{
    float4 position         : SV_Position;
    float3 worldPosition    : POSITION;
    float3 worldNormal      : NORMAL0;
    float2 uvs              : TEXCOORD0;
};

// Basic pixel shader color output:
struct PixelOutputBasic
{
    float4 color            : SV_Target0;
};

// Pixel shader entry point:
PixelOutputBasic MainPixel(const in VertexOutputBasic _vertexBasic)
{
    const float brightness = 0.5 + 0.5 * max(dot(_vertexBasic.worldNormal, float3(-0.57735, 0.57735, -0.57735)), 0);

    PixelOutputBasic o;
    o.color = float4(brightness, 0, brightness, 1); // lightly shaded magenta.
    return o;
}
