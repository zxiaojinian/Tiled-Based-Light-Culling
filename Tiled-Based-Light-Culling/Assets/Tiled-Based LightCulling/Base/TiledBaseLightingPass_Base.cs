using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

public class TiledBaseLightingPass_Base : TiledBaseLightingPass
{
    const string CMDSTR = "TiledBaseLightingPass_Base";


    RenderTargetIdentifier lightGrid;

    public TiledBaseLightingPass_Base()
    {
        lightGrid = new RenderTargetIdentifier(TiledBaseShaderPropertyId.id_LightGrid);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(CMDSTR);

        SetKeyWord(cmd);
        UpdateCommonData(ref renderingData, cmd);
        UpdateDepthBounds(cmd);

        //light culling
        ComputeBuffer lightIndexListBuffer = TiledBaseLightingBufferData.Instance.GetLightIndexList(MAX_NUM_LIGHTS_PER_TILE * tileCount);
        ComputeBuffer lightIndexCounterBuffer = TiledBaseLightingBufferData.Instance.GetLightIndexCounter();

        cmd.SetComputeMatrixParam(cs.LightCullingCS, TiledBaseShaderPropertyId.id_InverseProjection, invProjectionMatrix);
        cmd.SetComputeVectorParam(cs.LightCullingCS, TiledBaseShaderPropertyId.id_ScreenParams, screenParams);
        cmd.SetComputeIntParam(cs.LightCullingCS, TiledBaseShaderPropertyId.id_NumTilesX, tileCountX);
        cmd.SetComputeIntParam(cs.LightCullingCS, TiledBaseShaderPropertyId.id_NumTilesY, tileCountY);

        if (frustumsBuffer != null)
        {
            cmd.SetComputeBufferParam(cs.LightCullingCS, kernelLightCulling, TiledBaseShaderPropertyId.id_InFrustumsVS, frustumsBuffer);
        }
        cmd.SetComputeBufferParam(cs.LightCullingCS, kernelLightCulling, TiledBaseShaderPropertyId.id_LightsData, lightDataBuffer);
        cmd.SetComputeBufferParam(cs.LightCullingCS, kernelLightCulling, TiledBaseShaderPropertyId.id_LightIndexList, lightIndexListBuffer);
        cmd.SetComputeBufferParam(cs.LightCullingCS, kernelLightCulling, TiledBaseShaderPropertyId.id_LightIndexCounter, lightIndexCounterBuffer);

        cmd.GetTemporaryRT(TiledBaseShaderPropertyId.id_LightGrid, GetDescriptor(tileCountX, tileCountY, GraphicsFormat.R32G32B32A32_UInt), FilterMode.Point);
        cmd.SetComputeTextureParam(cs.LightCullingCS, kernelLightCulling, TiledBaseShaderPropertyId.id_InDepthBounds, depthBounds);
        cmd.SetComputeTextureParam(cs.LightCullingCS, kernelLightCulling, TiledBaseShaderPropertyId.id_LightGrid, lightGrid);
        if(isDebug)
        {
            cmd.GetTemporaryRT(TiledBaseShaderPropertyId.id_DebugTexture, pixelWidth, pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, true);
            cmd.SetComputeTextureParam(cs.LightCullingCS, kernelLightCulling, TiledBaseShaderPropertyId.id_DebugTexture, DebugTexture);
        }
        cmd.SetComputeIntParam(cs.LightCullingCS, TiledBaseShaderPropertyId.id_NumLights, additionalLightsCount);
        cmd.DispatchCompute(cs.LightCullingCS, kernelLightCulling, tileCountX, tileCountY, 1);

        //global data
        cmd.SetGlobalBuffer(TiledBaseShaderPropertyId.id_LightIndexList, lightIndexListBuffer);
        cmd.SetGlobalTexture(TiledBaseShaderPropertyId.id_LightGrid, lightGrid);
        cmd.SetGlobalInt(TiledBaseShaderPropertyId.id_NumTilesX, tileCountX);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        base.OnCameraCleanup(cmd);
        cmd.ReleaseTemporaryRT(TiledBaseShaderPropertyId.id_LightGrid);
        if (isDebug) cmd.ReleaseTemporaryRT(TiledBaseShaderPropertyId.id_DebugTexture);
    }

    protected override void SetKeyWord(CommandBuffer cmd)
    {
        base.SetKeyWord(cmd);

        if (isDebug)
        {
            cmd.EnableShaderKeyword("_LightCountDebug");
        }
        else
        {
            cmd.DisableShaderKeyword("_LightCountDebug");
        }
        cmd.DisableShaderKeyword("_LightCountDoubleDebug");
    }
}
