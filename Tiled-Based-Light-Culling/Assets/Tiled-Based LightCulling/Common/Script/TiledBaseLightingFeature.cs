using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum ZCull
{
    None,
    _2_5_D,
    HalfZ,
    modifiedHalfZ
}

public enum DebugType
{
    Tile,
    DoubleTile
}

public enum FrustumTest
{
    frustumPlane,
    frustumAABB,
    frustumPlaneAABBHybrid,
    frustumCone
}


[Serializable]
public class AllLightCullingCS
{
    public ComputeShader CalculateFrustumCS;
    public ComputeShader CalculateDepthBoundsCS;
    public ComputeShader LightCullingCS;

    public bool CSReady
    {
        get
        {
            return CalculateFrustumCS != null && CalculateDepthBoundsCS != null && LightCullingCS != null;
        }
    }
}

[Serializable]
public class TiledBaseLightingSetting
{
    public bool EnableTiledBaseLighting = true;
    public bool IsDebugGameView = false;
    public bool IsDebugSceneView = false;
    public Material DebugMaterial;
    [Range(0f, 1f)]
    public float DebugAlpha = 0.7f;

    public bool UseParallelReduction = true;
    public FrustumTest FrustumTestWay = FrustumTest.frustumPlane;
    public ZCull ZCullWay = ZCull.None;
    public DebugType DebugWay = DebugType.Tile;
    public AllLightCullingCS Base_CS;
    public AllLightCullingCS _25D_CS;
    public AllLightCullingCS HalfZ;
    public AllLightCullingCS MHalfZ;
}

public class TiledBaseLightingFeature : ScriptableRendererFeature
{
    public TiledBaseLightingSetting Setting = new TiledBaseLightingSetting();
    TiledBaseLightingPass_Base tiledBaseLightingPass_Base;
    TiledBaseLightingPass_25D tiledBaseLightingPass_25D;
    TiledBaseLightingPass_HalfZ tiledBaseLightingPass_HalfZ;
    TiledBaseLightingPass_MHalfZ tiledBaseLightingPass_MHalfZ;

    TiledBaseLightingDebugPass tiledBaseLightingDebugPass;

    public override void Create()
    {
        tiledBaseLightingPass_Base = new TiledBaseLightingPass_Base()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses
        };

        tiledBaseLightingPass_25D = new TiledBaseLightingPass_25D()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses
        };

        tiledBaseLightingPass_HalfZ = new TiledBaseLightingPass_HalfZ()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses
        };

        tiledBaseLightingPass_MHalfZ = new TiledBaseLightingPass_MHalfZ()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses
        };

        tiledBaseLightingDebugPass = new TiledBaseLightingDebugPass
        {
            renderPassEvent = RenderPassEvent.AfterRendering
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        TiledBaseLightingSetting setting;
        if (TiledBaseLightingConfig.Instance != null)
        {
            setting = TiledBaseLightingConfig.Instance.Setting;
        }
        else
        {
            setting = Setting;
        }

        if (!setting.EnableTiledBaseLighting) return;

        Camera camera = renderingData.cameraData.camera;
        bool mainGameCam = camera.cameraType == CameraType.Game && renderingData.cameraData.camera == Camera.main;

        bool debugResReady = setting.DebugMaterial != null;

        TiledBaseLightingPass pass = null;
        AllLightCullingCS cs = null;

        if(setting.ZCullWay == ZCull.None)
        {
            pass = tiledBaseLightingPass_Base;
            cs = setting.Base_CS;
        }
        else if(setting.ZCullWay == ZCull._2_5_D)
        {
            pass = tiledBaseLightingPass_25D;
            cs = setting._25D_CS;
        }
        else if(setting.ZCullWay == ZCull.HalfZ)
        {
            pass = tiledBaseLightingPass_HalfZ;
            cs = setting.HalfZ;
        }
        else if(setting.ZCullWay == ZCull.modifiedHalfZ)
        {
            pass = tiledBaseLightingPass_MHalfZ;
            cs = setting.MHalfZ;
        }


        bool resReady = cs != null && cs.CSReady;

        if (pass != null && resReady)
        {         
            if (mainGameCam)
            {
                if (debugResReady && setting.IsDebugGameView)
                {
                    pass.Setup(true, cs, setting);
                    renderer.EnqueuePass(pass);

                    tiledBaseLightingDebugPass.Setup(setting.DebugMaterial, setting.DebugAlpha);
                    renderer.EnqueuePass(tiledBaseLightingDebugPass);
                }
                else
                {
                    pass.Setup(false, cs, setting);
                    renderer.EnqueuePass(pass);
                }
            }
            else if (camera.cameraType == CameraType.SceneView)
            {
                if (debugResReady && setting.IsDebugSceneView)
                {
                    pass.Setup(true, cs, setting);
                    renderer.EnqueuePass(pass);

                    tiledBaseLightingDebugPass.Setup(setting.DebugMaterial, setting.DebugAlpha);
                    renderer.EnqueuePass(tiledBaseLightingDebugPass);
                }
                else
                {
                    pass.Setup(false, cs, setting);
                    renderer.EnqueuePass(pass);
                }
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        TiledBaseLightingBufferData.Instance.Dispose();
    }
}

