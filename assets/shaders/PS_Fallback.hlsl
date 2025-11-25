//<INC>
#include "defines/vertex_outputs/VertexOutputBasic.hlsl"
#include "defines/pixel_outputs/PixelOutputBasic.hlsl"
//</INC>

//<SHA>
// Pixel shader entry point:
PixelOutputBasic MainPixel(const in VertexOutputBasic _vertexBasic)
{
    const float brightness = 0.5 + 0.5 * max(dot(_vertexBasic.worldNormal, float3(-0.57735, 0.57735, -0.57735)), 0);

    PixelOutputBasic o;
    o.color = float4(brightness, 0, brightness, 1); // lightly shaded magenta.
    return o;
}
//</SHA>
