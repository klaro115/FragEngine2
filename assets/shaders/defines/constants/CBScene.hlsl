//<TYP>
// Constant buffer for scene-wide data.
cbuffer CBScene : register(b1)
{
    // Ambient lighting:
    float4 ambientLightTopDown;
    float4 ambientLightHorizon;
    float4 ambientLightBottomUp;

    // Scene contents:
    uint rendererCount;
    uint lightCount;
    uint lightCountShadowMapped;
    uint cameraCount;
    //...
};
//</TYP>
