#ifndef TILED_BASE_LIGHTING_INCLUDE
#define TILED_BASE_LIGHTING_INCLUDE

#include "../TiledBaseCSInclude.hlsl"

#if defined(_HALFZ) || defined(_MODIFIED_HALFZ)
    StructuredBuffer<uint> _LightIndexListDouble;
#else
    // Light index lists and light grids.
    StructuredBuffer<uint> _LightIndexList;
    Texture2D<uint2> _LightGrid;
#endif

struct TileLightData
{
    uint startOffset;
    uint lightCount;
};

TileLightData GetTileLightData(float4 postionSS, float3 postionVS)
{
    TileLightData data = (TileLightData)0;

#if defined(_HALFZ) || defined(_MODIFIED_HALFZ)
    uint2 tileIndex = uint2(floor(postionSS.xy / TILE_SIZE));
    uint tileFlatIndex = tileIndex.x + tileIndex.y * _NumTilesX;
    uint startIndex = (MAX_NUM_LIGHTS_PER_TILE  * 2 + 4) * tileFlatIndex;

    #if defined(_FRUSTUM_CONE)
        // 距离相机距离
        uint uHalfZBitsHigh = _LightIndexListDouble[startIndex];
        uint uHalfZBitsLow = _LightIndexListDouble[startIndex + 1];
        uint uHalfZBits = (uHalfZBitsHigh << 16) | uHalfZBitsLow;
        float fHalfZ = asfloat(uHalfZBits);//正数
        //float fHalfZ = asfloat(_LightIndexListDouble[startIndex]);//正数
        float fViewPosDis = length(postionVS);

        data.startOffset = (fViewPosDis < fHalfZ) ? (startIndex + 4) : (startIndex + 4 + MAX_NUM_LIGHTS_PER_TILE);
        data.lightCount = (fViewPosDis < fHalfZ) ? _LightIndexListDouble[startIndex + 2] : _LightIndexListDouble[startIndex + 3];
        data.lightCount = min(data.lightCount, MAX_NUM_LIGHTS_PER_TILE);

    #else
        // reconstruct fHalfZ
        uint uHalfZBitsHigh = _LightIndexListDouble[startIndex];
        uint uHalfZBitsLow = _LightIndexListDouble[startIndex + 1];
        uint uHalfZBits = (uHalfZBitsHigh << 16) | uHalfZBitsLow;
        float fHalfZ = asfloat(uHalfZBits);//负数
        //float fHalfZ = asfloat(_LightIndexListDouble[startIndex]);//负数
        float fViewPosZ = postionVS.z;

        data.startOffset = (fViewPosZ > fHalfZ) ? (startIndex + 4) : (startIndex + 4 + MAX_NUM_LIGHTS_PER_TILE);
        data.lightCount = (fViewPosZ > fHalfZ) ? _LightIndexListDouble[startIndex + 2] : _LightIndexListDouble[startIndex + 3];
        data.lightCount = min(data.lightCount, MAX_NUM_LIGHTS_PER_TILE);
    #endif
#else
    uint2 tileIndex = uint2(floor(postionSS.xy / TILE_SIZE));
    uint2 lightGridData = _LightGrid[tileIndex];

    data.startOffset = lightGridData.x;
    data.lightCount = min(lightGridData.y, MAX_NUM_LIGHTS_PER_TILE);
#endif
    return data;
}

uint GetTileAddtionalLightIndex(TileLightData data, int index)
{
#if defined(_HALFZ) || defined(_MODIFIED_HALFZ)
    return _LightIndexListDouble[data.startOffset + index];
#else
    return _LightIndexList[data.startOffset + index];
#endif
}

#endif