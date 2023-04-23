using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class TiledBaseLightingDebugPass : ScriptableRenderPass
{
    Material debugMaterial;
    float debugAlpha;

    public void Setup(Material mat, float alpha)
    {
        debugMaterial = mat;
        debugAlpha = alpha;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("TiledBaseLightingDebugPass");
        cmd.SetGlobalFloat(TiledBaseShaderPropertyId.id_DebugAlpha, debugAlpha);
        cmd.DrawProcedural(Matrix4x4.identity, debugMaterial, 0, MeshTopology.Triangles, 3);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
