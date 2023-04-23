using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections;

public class TiledBaseLightingBufferData : IDisposable
{
    static TiledBaseLightingBufferData instance = null;

    ComputeBuffer frustumsVSBuffer;
    ComputeBuffer frustumConesVSBuffer;

    ComputeBuffer lightSphereBuffer;
    ComputeBuffer lightConeBuffer;

    ComputeBuffer lightIndexList;
    ComputeBuffer lightIndexCounter;
    ComputeBuffer lightIndexListDouble;

    uint[] counterDefault = new uint[1] {0};

    public static TiledBaseLightingBufferData Instance
    {
        get
        {
            if (instance == null)
                instance = new TiledBaseLightingBufferData();

            return instance;
        }
    }

    public ComputeBuffer GetFrustumsVSBuffer(int size)
    {
        return GetOrUpdateBuffer<Frustum>(ref frustumsVSBuffer, size);
    }

    public ComputeBuffer GetFrustumConesVSBuffer(int size)
    {
        return GetOrUpdateBuffer<FrustumCone>(ref frustumConesVSBuffer, size);
    }

    public ComputeBuffer GetLightSphereBuffer(int size)
    {
        return GetOrUpdateBuffer<LightSphere>(ref lightSphereBuffer, size);
    }

    public ComputeBuffer GetLightConeBuffer(int size)
    {
        return GetOrUpdateBuffer<LightCone>(ref lightConeBuffer, size);
    }

    public ComputeBuffer GetLightIndexList(int size)
    {
        return GetOrUpdateBuffer<uint>(ref lightIndexList, size);
    }


    public ComputeBuffer GetLightIndexListDouble(int size)
    {
        return GetOrUpdateBuffer<uint>(ref lightIndexListDouble, size);
    }

    public ComputeBuffer GetLightIndexCounter()
    {
        GetOrUpdateBuffer<uint>(ref lightIndexCounter, 1).SetData(counterDefault);
        return lightIndexCounter;
    }

    ComputeBuffer GetOrUpdateBuffer<T>(ref ComputeBuffer buffer, int size) where T : struct
    {
        if (buffer == null)
        {
            buffer = new ComputeBuffer(size, Marshal.SizeOf<T>());
        }
        else if (size > buffer.count)
        {
            buffer.Dispose();
            buffer = new ComputeBuffer(size, Marshal.SizeOf<T>());
        }

        return buffer;
    }

    public void Dispose()
    {
        DisposeBuffer(ref frustumsVSBuffer);
        DisposeBuffer(ref frustumConesVSBuffer);
        DisposeBuffer(ref lightSphereBuffer);
        DisposeBuffer(ref lightConeBuffer);
        DisposeBuffer(ref lightIndexList);
        DisposeBuffer(ref lightIndexCounter);
        DisposeBuffer(ref lightIndexListDouble);
    }

    void DisposeBuffer(ref ComputeBuffer buffer)
    {
        if (buffer != null)
        {
            buffer.Dispose();
            buffer = null;
        }
    }


    public struct LightSphere
    {
        public Vector3 c;
        public float r;
    }

    public struct LightCone
    {
        public Vector4 ConeParams;  //灯光包围圆锥数据(lightDistSqr, lightDist, lightSin ,lightCos)，圆锥顶点在相机位置
        public Vector4 ConeDir; //xyz灯光包围圆锥方向，圆锥顶点在相机位置,z,range
    }

    public struct Frustum
    {
        public Vector4 L;
        public Vector4 R;
        public Vector4 T;
        public Vector4 B;
    }

    public struct FrustumCone
    {
        public Vector3 coneDir;
        public Vector2 coneSinCos;
    };
}
