using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

public class TiledBaseLightingPass_HalfZ : TiledBaseLightingPass
{
    const string CMDSTR = "LightCullingCS_HalfZ";


    public TiledBaseLightingPass_HalfZ()
    {
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(CMDSTR);
        CameraData cameraData = renderingData.cameraData;

        SetKeyWord(cmd);
        UpdateCommonData(ref renderingData, cmd);
        UpdateDepthBounds(cmd);

        //light culling
        int lightIndexListCountDouble = (2 * MAX_NUM_LIGHTS_PER_TILE + 4) * tileCount;
        ComputeBuffer lightIndexListDoubleBuffer = TiledBaseLightingBufferData.Instance.GetLightIndexListDouble(lightIndexListCountDouble);

        cmd.SetComputeMatrixParam(cs.LightCullingCS, TiledBaseShaderPropertyId.id_InverseProjection, invProjectionMatrix);
        cmd.SetComputeVectorParam(cs.LightCullingCS, TiledBaseShaderPropertyId.id_ScreenParams, screenParams);
        cmd.SetComputeIntParam(cs.LightCullingCS, TiledBaseShaderPropertyId.id_NumTilesX, tileCountX);
        cmd.SetComputeIntParam(cs.LightCullingCS, TiledBaseShaderPropertyId.id_NumTilesY, tileCountY);

        if (frustumsBuffer != null)
        {
            cmd.SetComputeBufferParam(cs.LightCullingCS, kernelLightCulling, TiledBaseShaderPropertyId.id_InFrustumsVS, frustumsBuffer);
        }
        cmd.SetComputeBufferParam(cs.LightCullingCS, kernelLightCulling, TiledBaseShaderPropertyId.id_LightsData, lightDataBuffer);
        cmd.SetComputeBufferParam(cs.LightCullingCS, kernelLightCulling, TiledBaseShaderPropertyId.id_LightIndexListDouble, lightIndexListDoubleBuffer);

        cmd.SetComputeTextureParam(cs.LightCullingCS, kernelLightCulling, TiledBaseShaderPropertyId.id_InDepthBounds, depthBounds);
        if(isDebug)
        {
            cmd.GetTemporaryRT(TiledBaseShaderPropertyId.id_DebugTexture, pixelWidth, pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, true);
            cmd.SetComputeTextureParam(cs.LightCullingCS, kernelLightCulling, TiledBaseShaderPropertyId.id_DebugTexture, DebugTexture);
        }
        cmd.SetComputeIntParam(cs.LightCullingCS, TiledBaseShaderPropertyId.id_NumLights, additionalLightsCount);
        cmd.DispatchCompute(cs.LightCullingCS, kernelLightCulling, tileCountX, tileCountY, 1);

        //global data
        cmd.SetGlobalBuffer(TiledBaseShaderPropertyId.id_LightIndexListDouble, lightIndexListDoubleBuffer);
        cmd.SetGlobalInt(TiledBaseShaderPropertyId.id_NumTilesX, tileCountX);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        base.OnCameraCleanup(cmd);
        if (isDebug) cmd.ReleaseTemporaryRT(TiledBaseShaderPropertyId.id_DebugTexture);
    }

    protected override void SetKeyWord(CommandBuffer cmd)
    {
        base.SetKeyWord(cmd);

        if (isDebug)
        {

            if (debugType == DebugType.Tile)
            {
                cmd.EnableShaderKeyword("_LightCountDebug");
                cmd.DisableShaderKeyword("_LightCountDoubleDebug");
            }
            else if (debugType == DebugType.DoubleTile)
            {
                cmd.DisableShaderKeyword("_LightCountDebug");
                cmd.EnableShaderKeyword("_LightCountDoubleDebug");
            }
        }
        else
        {
            cmd.DisableShaderKeyword("_LightCountDebug");
            cmd.DisableShaderKeyword("_LightCountDoubleDebug");
        }
    }
}
