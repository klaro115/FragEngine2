//<TYP>
// Constant buffer for per-camera data.
cbuffer CBCamera : register(b2)
{
    // Projection:
    float4x4 mtxWorld2Clip;
    float4x4 mtxClip2World;

    // Clearing:
    float4 backgroundColor;

    // Output format:
    uint cameraIndex;
    uint cameraPassIndex;
    uint resolutionX;
    uint resolutionY;
    float nearClipPlane;
    float farClipPlane;
    //...
};
//</TYP>
