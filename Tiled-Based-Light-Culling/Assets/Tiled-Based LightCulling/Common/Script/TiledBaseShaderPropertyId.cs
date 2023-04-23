using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TiledBaseShaderPropertyId
{
    public static readonly int id_InverseProjection = Shader.PropertyToID("_InverseProjection");
    public static readonly int id_ScreenParams = Shader.PropertyToID("_ScreenParams_CS");
    public static readonly int id_NumTilesX = Shader.PropertyToID("_NumTilesX");
    public static readonly int id_NumTilesY = Shader.PropertyToID("_NumTilesY");

    public static readonly int id_OutFrustumsVS = Shader.PropertyToID("_OutFrustumsVS");

    public static readonly int id_DepthTexture = Shader.PropertyToID("_DepthTexture");
    public static readonly int id_OutDepthBounds = Shader.PropertyToID("_OutDepthBounds");

    public static readonly int id_NumLights = Shader.PropertyToID("_NumLights");
    public static readonly int id_LightsData = Shader.PropertyToID("_LightsData");
    public static readonly int id_InDepthBounds = Shader.PropertyToID("_InDepthBounds");
    public static readonly int id_InFrustumsVS = Shader.PropertyToID("_InFrustumsVS");
    public static readonly int id_LightIndexList = Shader.PropertyToID("_LightIndexList");
    public static readonly int id_LightGrid = Shader.PropertyToID("_LightGrid");
    public static readonly int id_LightIndexCounter = Shader.PropertyToID("_LightIndexCounter");
    public static readonly int id_LightIndexListDouble = Shader.PropertyToID("_LightIndexListDouble");

    public static readonly int id_DebugTexture = Shader.PropertyToID("_DebugTexture");
    public static readonly int id_DebugAlpha = Shader.PropertyToID("_DebugAlpha");

}
