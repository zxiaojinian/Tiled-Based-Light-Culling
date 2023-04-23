using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class TiledBaseLightingConfig : MonoBehaviour
{
    static TiledBaseLightingConfig instance;

    public static TiledBaseLightingConfig Instance
    {
        get
        {
            return instance;
        }
    }

    public TiledBaseLightingSetting Setting;

    private void OnEnable()
    {
        instance = this;
    }

    private void OnDisable()
    {
        instance = null;
    }

    public void SetEnable(bool value)
    {
        Setting.EnableTiledBaseLighting = value;
    }

    public void SetIsDebugGameView(bool value)
    {
        Setting.IsDebugGameView = value;
    }

    public void SetDebugAlpha(float value)
    {
        Setting.DebugAlpha = value;
    }
    
    public void SetUseParallelReduction(bool value)
    {
        Setting.UseParallelReduction = value;
    }

    public void SetFrustumTest(int value)
    {
        Setting.FrustumTestWay = (FrustumTest)value;
    }

    public void SetZCullWay(int value)
    {
        Setting.ZCullWay = (ZCull)value;
    }

    public void SetDebugWay(int value)
    {
        Setting.DebugWay = (DebugType)value;
    }
}
