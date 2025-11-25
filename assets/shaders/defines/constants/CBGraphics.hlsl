//<TYP>
// Constant buffer for engine-wide graphics data.
cbuffer CBGraphics : register(b0)
{
    // Time data:
    float appTime;
    float levelTime;
    float ingameTime;

    float deltaTime;
    float frameRate;
    uint frameIndex;

    // Graphics data:
    uint windowCount;
    uint sceneCount;
    uint cameraCount;
    //...
};
//</TYP>
