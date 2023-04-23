using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

public abstract class TiledBaseLightingPass : ScriptableRenderPass
{
    protected const int TILE_SIZE = 16;
    protected const float INV_TILE_SIZE = 1.0f / TILE_SIZE;
    protected const int MAX_NUM_LIGHTS_PER_TILE = 32;

    protected int kernelCalculateFrustum;
    protected int kernelLightCulling;
    protected int kernelCalculateDepthBounds;

    protected RenderTargetIdentifier depthBounds;
    protected RenderTargetIdentifier depthTexture;
    protected RenderTargetIdentifier DebugTexture;

    protected bool isDebug;
    protected DebugType debugType;
    protected AllLightCullingCS cs;
    protected FrustumTest frustumTestWay;
    protected bool useParallelReduction;
    protected ZCull zCullType;

    protected int pixelWidth = -1;
    protected int pixelHeight = -1;
    protected Vector4 screenParams;
    protected int tileCountX;
    protected int tileCountY;
    protected int tileCount;

    protected int additionalLightsCount;
    protected ComputeBuffer lightDataBuffer;

    private ComputeBuffer frustumsVsBuffer;
    private ComputeBuffer frustumsConesVsBuffer;
    protected ComputeBuffer frustumsBuffer;

    private Matrix4x4 preProjectionMatrix = Matrix4x4.identity;
    protected Matrix4x4 invProjectionMatrix;

    public TiledBaseLightingPass()
    {
        depthBounds = new RenderTargetIdentifier(TiledBaseShaderPropertyId.id_OutDepthBounds);
        depthTexture = new RenderTargetIdentifier("_CameraDepthTexture");
        DebugTexture = new RenderTargetIdentifier(TiledBaseShaderPropertyId.id_DebugTexture);
    }

    public virtual void Setup(bool debug, AllLightCullingCS cs, TiledBaseLightingSetting setting)
    {
        isDebug = debug;
        this.cs = cs;
        debugType = setting.DebugWay;
        frustumTestWay = setting.FrustumTestWay;
        useParallelReduction = setting.UseParallelReduction;
        zCullType = setting.ZCullWay;

        kernelCalculateFrustum = cs.CalculateFrustumCS.FindKernel("CalculateFrustumCS");
        kernelCalculateDepthBounds = cs.CalculateDepthBoundsCS.FindKernel("CalculateDepthBoundsCS");
        kernelLightCulling = cs.LightCullingCS.FindKernel("LightCullingCS");
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        if (cmd == null) return;
        cmd.ReleaseTemporaryRT(TiledBaseShaderPropertyId.id_OutDepthBounds);
        cmd.DisableShaderKeyword("_TILED_BASE_LIGHTING");
    }

    protected RenderTextureDescriptor GetDescriptor(int width, int height, GraphicsFormat graphicsFormat)
    {
        RenderTextureDescriptor desc = new RenderTextureDescriptor(width, height);
        desc.msaaSamples = 1;
        desc.depthBufferBits = 0;
        desc.graphicsFormat = graphicsFormat;
        desc.useMipMap = false;
        desc.autoGenerateMips = false;
        desc.enableRandomWrite = true;
        return desc;
    }

    protected void UpdateCommonData(ref RenderingData renderingData, CommandBuffer cmd)
    {
        CameraData cameraData = renderingData.cameraData;

        bool needUpdate = false;
        if(pixelWidth != cameraData.cameraTargetDescriptor.width || pixelHeight != cameraData.cameraTargetDescriptor.height)
        {
            needUpdate = true;

            pixelWidth = cameraData.cameraTargetDescriptor.width;
            pixelHeight = cameraData.cameraTargetDescriptor.height;
            screenParams = new Vector4(pixelWidth, pixelHeight, 1.0f / pixelWidth, 1.0f / pixelHeight);
            tileCountX = Mathf.CeilToInt(INV_TILE_SIZE * pixelWidth);
            tileCountY = Mathf.CeilToInt(INV_TILE_SIZE * pixelHeight);
            tileCount = tileCountX * tileCountY;
        }

        if(preProjectionMatrix != cameraData.camera.projectionMatrix)
        {
            needUpdate = true;
            preProjectionMatrix = cameraData.camera.projectionMatrix;

            invProjectionMatrix = Matrix4x4.Inverse(cameraData.camera.projectionMatrix);
        }

        needUpdate = needUpdate || (frustumTestWay == FrustumTest.frustumCone ? frustumsConesVsBuffer == null : frustumsVsBuffer == null);
        //aabb不需要计算视锥体
        if (frustumTestWay == FrustumTest.frustumAABB)
        {
            frustumsBuffer = null;
        }
        else
        {
            if (needUpdate)
            {
                cmd.SetComputeMatrixParam(cs.CalculateFrustumCS, TiledBaseShaderPropertyId.id_InverseProjection, invProjectionMatrix);
                cmd.SetComputeVectorParam(cs.CalculateFrustumCS, TiledBaseShaderPropertyId.id_ScreenParams, screenParams);
                cmd.SetComputeIntParam(cs.CalculateFrustumCS, TiledBaseShaderPropertyId.id_NumTilesX, tileCountX);
                cmd.SetComputeIntParam(cs.CalculateFrustumCS, TiledBaseShaderPropertyId.id_NumTilesY, tileCountY);

                if (frustumTestWay == FrustumTest.frustumCone)
                {
                    frustumsConesVsBuffer = TiledBaseLightingBufferData.Instance.GetFrustumConesVSBuffer(tileCount);
                    frustumsBuffer = frustumsConesVsBuffer;
                }
                else
                {
                    frustumsVsBuffer = TiledBaseLightingBufferData.Instance.GetFrustumsVSBuffer(tileCount);
                    frustumsBuffer = frustumsVsBuffer;
                }
                cmd.SetComputeBufferParam(cs.CalculateFrustumCS, kernelCalculateFrustum, TiledBaseShaderPropertyId.id_OutFrustumsVS, frustumsBuffer);

                int frustumGroupX = Mathf.CeilToInt(tileCountX * INV_TILE_SIZE);
                int frustumGroupY = Mathf.CeilToInt(tileCountY * INV_TILE_SIZE);
                cmd.DispatchCompute(cs.CalculateFrustumCS, kernelCalculateFrustum, frustumGroupX, frustumGroupY, 1);
            }
            else
            {
                if (frustumTestWay == FrustumTest.frustumCone)
                {
                    frustumsConesVsBuffer = TiledBaseLightingBufferData.Instance.GetFrustumConesVSBuffer(tileCount);
                    frustumsBuffer = frustumsConesVsBuffer;
                }
                else
                {
                    frustumsVsBuffer = TiledBaseLightingBufferData.Instance.GetFrustumsVSBuffer(tileCount);
                    frustumsBuffer = frustumsVsBuffer;
                }
            }
        }

        lightDataBuffer = GetLightDataBuffer(ref renderingData.lightData, cameraData.camera, out additionalLightsCount);
    }

    protected void UpdateDepthBounds(CommandBuffer cmd)
    {
        cmd.SetComputeMatrixParam(cs.CalculateDepthBoundsCS, TiledBaseShaderPropertyId.id_InverseProjection, invProjectionMatrix);
        cmd.SetComputeVectorParam(cs.CalculateDepthBoundsCS, TiledBaseShaderPropertyId.id_ScreenParams, screenParams);
        cmd.GetTemporaryRT(TiledBaseShaderPropertyId.id_OutDepthBounds, GetDescriptor(tileCountX, tileCountY, GraphicsFormat.R32G32B32A32_SFloat), FilterMode.Point);
        cmd.SetComputeTextureParam(cs.CalculateDepthBoundsCS, kernelCalculateDepthBounds, TiledBaseShaderPropertyId.id_DepthTexture, depthTexture);
        cmd.SetComputeTextureParam(cs.CalculateDepthBoundsCS, kernelCalculateDepthBounds, TiledBaseShaderPropertyId.id_OutDepthBounds, depthBounds);
        cmd.DispatchCompute(cs.CalculateDepthBoundsCS, kernelCalculateDepthBounds, tileCountX, tileCountY, 1);
    }

    protected virtual void SetKeyWord(CommandBuffer cmd)
    {
        if (cmd == null) return;

        cmd.EnableShaderKeyword("_TILED_BASE_LIGHTING");

        if(useParallelReduction)
        {
            cmd.EnableShaderKeyword("_PARALLELREDUCTION");
        }
        else
        {
            cmd.DisableShaderKeyword("_PARALLELREDUCTION");
        }

        if(frustumTestWay == FrustumTest.frustumPlane)
        {
            cmd.EnableShaderKeyword("_FRUSTUM_PLANE");
            cmd.DisableShaderKeyword("_FRUSTUM_AABB");
            cmd.DisableShaderKeyword("_FRUSTUM_PLANE_AABB_HYBRID");
            cmd.DisableShaderKeyword("_FRUSTUM_CONE");
        }
        else if(frustumTestWay == FrustumTest.frustumAABB)
        {
            cmd.DisableShaderKeyword("_FRUSTUM_PLANE");
            cmd.EnableShaderKeyword("_FRUSTUM_AABB");
            cmd.DisableShaderKeyword("_FRUSTUM_PLANE_AABB_HYBRID");
            cmd.DisableShaderKeyword("_FRUSTUM_CONE");
        }
        else if (frustumTestWay == FrustumTest.frustumPlaneAABBHybrid)
        {
            cmd.DisableShaderKeyword("_FRUSTUM_PLANE");
            cmd.DisableShaderKeyword("_FRUSTUM_AABB");
            cmd.EnableShaderKeyword("_FRUSTUM_PLANE_AABB_HYBRID");
            cmd.DisableShaderKeyword("_FRUSTUM_CONE");
        }
        else if(frustumTestWay == FrustumTest.frustumCone)
        {
            cmd.DisableShaderKeyword("_FRUSTUM_PLANE");
            cmd.DisableShaderKeyword("_FRUSTUM_AABB");
            cmd.DisableShaderKeyword("_FRUSTUM_PLANE_AABB_HYBRID");
            cmd.EnableShaderKeyword("_FRUSTUM_CONE");
        }

        if (zCullType == ZCull.None)
        {
            cmd.DisableShaderKeyword("_2_5_D");
            cmd.DisableShaderKeyword("_HALFZ");
            cmd.DisableShaderKeyword("_MODIFIED_HALFZ");
        }
        else if (zCullType == ZCull._2_5_D)
        {
            cmd.EnableShaderKeyword("_2_5_D");
            cmd.DisableShaderKeyword("_HALFZ");
            cmd.DisableShaderKeyword("_MODIFIED_HALFZ");
        }
        else if (zCullType == ZCull.HalfZ)
        {
            cmd.DisableShaderKeyword("_2_5_D");
            cmd.EnableShaderKeyword("_HALFZ");
            cmd.DisableShaderKeyword("_MODIFIED_HALFZ");
        }
        else if (zCullType == ZCull.modifiedHalfZ)
        {
            cmd.DisableShaderKeyword("_2_5_D");
            cmd.DisableShaderKeyword("_HALFZ");
            cmd.EnableShaderKeyword("_MODIFIED_HALFZ");
        }
    }

    protected void SetLightSphereData(ref VisibleLight light, out TiledBaseLightingBufferData.LightSphere lightData, Camera cam)
    {
        lightData = new TiledBaseLightingBufferData.LightSphere();
        if (light.lightType == LightType.Point)
        {
            Vector4 posWS = light.localToWorldMatrix.GetColumn(3);
            Vector4 posVS = cam.worldToCameraMatrix.MultiplyPoint(posWS);
            lightData.c = posVS;
            lightData.r = light.range;
        }
        else if (light.lightType == LightType.Spot)
        {
            Vector4 posWS = light.localToWorldMatrix.GetColumn(3);
            Vector4 posVS = cam.worldToCameraMatrix.MultiplyPoint(posWS);
            Vector4 dirWS = light.localToWorldMatrix.GetColumn(2);
            Vector4 dirVS = cam.worldToCameraMatrix.MultiplyVector(dirWS);
            float spotlightAngle = Mathf.Deg2Rad * light.spotAngle * 0.5f;
            //最小包围球
            if (light.spotAngle >= 90f)
            {
                float sphereRadius = Mathf.Tan(spotlightAngle) * light.range;//钝角三角形最小覆盖圆直径为最长边
                Vector4 sphereCenterVS = posVS + dirVS * light.range;
                lightData.c = sphereCenterVS;
                lightData.r = sphereRadius;
            }
            else
            {
                //https://ubm-twvideo01.s3.amazonaws.com/o1/vault/gdc2015/presentations/Thomas_Gareth_Advancements_in_Tile-Based.pdf 示意图
                //https://wickedengine.net/2018/01/10/optimizing-tile-based-light-culling/
                float spotLightConeHalfAngleCos = Mathf.Cos(spotlightAngle);
                float sphereRadius = light.range * 0.5f / (spotLightConeHalfAngleCos * spotLightConeHalfAngleCos); //锐角三角形最小覆盖圆为其外接圆
                Vector4 sphereCenterVS = posVS + dirVS * sphereRadius;
                lightData.c = sphereCenterVS;
                lightData.r = sphereRadius;
            }
        }
    }

    protected void SetLightConeData(ref VisibleLight light, out TiledBaseLightingBufferData.LightCone lightData, Camera cam)
    {
        lightData = new TiledBaseLightingBufferData.LightCone();
        if (light.lightType == LightType.Point)
        {
            Vector4 posWS = light.localToWorldMatrix.GetColumn(3);
            Vector4 posVS = cam.worldToCameraMatrix.MultiplyPoint(posWS);
            float lightDistSqr = Vector3.Dot(posVS, posVS);
            float lightDist = Mathf.Sqrt(lightDistSqr);
            Vector3 coneDir = posVS / lightDist;
            float lightSin = Mathf.Clamp01(light.range / lightDist);
            float lightCos = Mathf.Sqrt(1 - lightSin * lightSin);

            lightData.ConeParams = new Vector4(lightDistSqr, lightDist, lightSin, lightCos);
            lightData.ConeDir = new Vector4(coneDir.x, coneDir.y, coneDir.z, light.range);
        }
        else if (light.lightType == LightType.Spot)
        {
            Vector4 posWS = light.localToWorldMatrix.GetColumn(3);
            Vector4 posVS = cam.worldToCameraMatrix.MultiplyPoint(posWS);
            Vector4 dirWS = light.localToWorldMatrix.GetColumn(2);
            Vector4 dirVS = cam.worldToCameraMatrix.MultiplyVector(dirWS);
            float spotlightAngle = Mathf.Deg2Rad * light.spotAngle * 0.5f;
            float sphereRadius;
            Vector4 sphereCenterVS;
            //最小包围球
            if (light.spotAngle >= 90f)
            {
                sphereRadius = Mathf.Tan(spotlightAngle) * light.range;//钝角三角形最小覆盖圆直径为最长边
                sphereCenterVS = posVS + dirVS * light.range;
            }
            else
            {
                //https://ubm-twvideo01.s3.amazonaws.com/o1/vault/gdc2015/presentations/Thomas_Gareth_Advancements_in_Tile-Based.pdf 示意图
                //https://wickedengine.net/2018/01/10/optimizing-tile-based-light-culling/
                float spotLightConeHalfAngleCos = Mathf.Cos(spotlightAngle);
                sphereRadius = light.range * 0.5f / (spotLightConeHalfAngleCos * spotLightConeHalfAngleCos); //锐角三角形最小覆盖圆为其外接圆
                sphereCenterVS = posVS + dirVS * sphereRadius;
            }

            float lightDistSqr = Vector3.Dot(sphereCenterVS, sphereCenterVS);
            float lightDist = Mathf.Sqrt(lightDistSqr);
            Vector3 coneDir = sphereCenterVS / lightDist;
            float lightSin = Mathf.Clamp01(sphereRadius / lightDist);
            float lightCos = Mathf.Sqrt(1 - lightSin * lightSin);
            lightData.ConeParams = new Vector4(lightDistSqr, lightDist, lightSin, lightCos);
            lightData.ConeDir = new Vector4(coneDir.x, coneDir.y, coneDir.z, sphereRadius);
        }
    }

    protected ComputeBuffer GetLightDataBuffer(ref LightData lightData, Camera camera, out int additionalLightsCount)
    {
        ComputeBuffer lightDataBuffer;

        var lights = lightData.visibleLights;
        additionalLightsCount = Mathf.Max(0, lights.Length - 1);

        if (frustumTestWay == FrustumTest.frustumCone)
        {
            NativeArray<TiledBaseLightingBufferData.LightCone> additionalLightsData = new NativeArray<TiledBaseLightingBufferData.LightCone>(additionalLightsCount, Allocator.Temp);
            for (int i = 0, lightIter = 0; i < lights.Length && lightIter < additionalLightsCount; ++i)
            {
                VisibleLight light = lights[i];
                if (lightData.mainLightIndex != i)
                {
                    SetLightConeData(ref light, out TiledBaseLightingBufferData.LightCone data, camera);
                    additionalLightsData[lightIter++] = data;
                }
            }
            lightDataBuffer = TiledBaseLightingBufferData.Instance.GetLightConeBuffer(additionalLightsCount);
            lightDataBuffer.SetData(additionalLightsData);
            additionalLightsData.Dispose();
        }
        else
        {
            NativeArray<TiledBaseLightingBufferData.LightSphere> additionalLightsData = new NativeArray<TiledBaseLightingBufferData.LightSphere>(additionalLightsCount, Allocator.Temp);
            for (int i = 0, lightIter = 0; i < lights.Length && lightIter < additionalLightsCount; ++i)
            {
                VisibleLight light = lights[i];
                if (lightData.mainLightIndex != i)
                {
                    SetLightSphereData(ref light, out TiledBaseLightingBufferData.LightSphere data, camera);
                    additionalLightsData[lightIter++] = data;
                }
            }
            lightDataBuffer = TiledBaseLightingBufferData.Instance.GetLightSphereBuffer(additionalLightsCount);
            lightDataBuffer.SetData(additionalLightsData);
            additionalLightsData.Dispose();
        }

        return lightDataBuffer;
    }
}
