using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    private const string BufferName = "Lighting";
    private const int MaxDirLightCount = 4, MaxOtherLightCount = 64;

    private CommandBuffer _buffer = new CommandBuffer
    {
        name = BufferName
    };

    private static int
        _dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
        _dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),
        _dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections"),
        _dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

    private static int
        _otherLightCountId = Shader.PropertyToID("_OtherLightCount"),
        _otherLightColorsId = Shader.PropertyToID("_OtherLightColors"),
        _otherLightPositionsId = Shader.PropertyToID("_OtherLightPositions"),
        _otherLightDirectionsId = Shader.PropertyToID("_OtherLightDirections"),
        _otherLightSpotAnglesId = Shader.PropertyToID("_OtherLightSpotAngles"),
        _otherLightShadowDataId = Shader.PropertyToID("_OtherLightShadowData");
    
    private static Vector4[]
        _dirLightColors = new Vector4[MaxDirLightCount],
        _dirLightDirections = new Vector4[MaxDirLightCount],
        _dirLightShadowData = new Vector4[MaxDirLightCount];

    private static Vector4[]
        _otherLightColors = new Vector4[MaxOtherLightCount],
        _otherLightPositions = new Vector4[MaxOtherLightCount],
        _otherLightDirections = new Vector4[MaxOtherLightCount],
        _otherLightSpotAngles = new Vector4[MaxOtherLightCount],
        _otherLightShadowData = new Vector4[MaxOtherLightCount];

    private static string _lightsPerObjectKeyword = "_LIGHTS_PER_OBJECT";

    private CullingResults _cullingResults;

    private Shadows _shadows = new Shadows();
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings, bool useLightsPerObject)
    {
        this._cullingResults = cullingResults;
        _buffer.BeginSample(BufferName);
        _shadows.Setup(context, cullingResults, shadowSettings);
        SetupLights(useLightsPerObject);
        _shadows.Render();
        //SetupDirectionalLight();
        _buffer.EndSample(BufferName);
        context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }

    void SetupDirectionalLight(int index, int visibleIndex, ref VisibleLight visibleLight)
    {
        _dirLightColors[index] = visibleLight.finalColor;
        _dirLightDirections[index] = - visibleLight.localToWorldMatrix.GetColumn(2);
        _dirLightShadowData[index] = _shadows.ReserveDirectionalShadows(visibleLight.light, visibleIndex);
    }

    void SetupPointLight(int index, int visibleIndex, ref VisibleLight visibleLight)
    {
        _otherLightColors[index] = visibleLight.finalColor;
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        _otherLightPositions[index] = position;
        _otherLightSpotAngles[index] = new Vector4(0f, 1f);
        Light light = visibleLight.light;
        _otherLightShadowData[index] = _shadows.ReserveOtherShadows(light, visibleIndex);
    }

    void SetupSpotLights(int index, int visibleIndex, ref VisibleLight visibleLight)
    {
        _otherLightColors[index] = visibleLight.finalColor;
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        _otherLightPositions[index] = position;
        _otherLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        Light light = visibleLight.light;
        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
        float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
        float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
        _otherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
        _otherLightShadowData[index] = _shadows.ReserveOtherShadows(light, visibleIndex);
    }

    void SetupLights(bool useLightsPerObject)
    {
        NativeArray<int> indexMap = useLightsPerObject ?
            _cullingResults.GetLightIndexMap(Allocator.Temp) : default;
        NativeArray<VisibleLight> visibleLights = _cullingResults.visibleLights;

        int dirLightCount = 0, otherLightCount = 0;
        int i;
        for (i = 0; i < visibleLights.Length; i++)
        {
            int newIndex = -1;
            VisibleLight visibleLight = visibleLights[i];
            //  Recognize light type is directional light
            // if (visibleLight.lightType == LightType.Directional)
            // {
            //     if (dirLightCount >= MaxDirLightCount)
            //     {
            //         Debug.LogError("The maximum number of directional lights is only supported 4");
            //         break;
            //     }
            switch (visibleLight.lightType)
            {
                case LightType.Directional:
                    if (dirLightCount < MaxDirLightCount)
                    {
                        SetupDirectionalLight(dirLightCount++, i, ref visibleLight);
                    }
                    else
                    {
                        Debug.LogError("The maximum number of directional lights is only supported 4");
                    }
                    break;
                case LightType.Point:
                    if (otherLightCount < MaxOtherLightCount)
                    {
                        newIndex = otherLightCount;
                        SetupPointLight(otherLightCount++, i, ref visibleLight );
                    }
                    break;
                case LightType.Spot:
                    if (otherLightCount < MaxOtherLightCount)
                    {
                        newIndex = otherLightCount;
                        SetupSpotLights(otherLightCount++, i, ref visibleLight);
                    }
                    break;
            }
        }
        
        if (useLightsPerObject)
        {
            for (; i < indexMap.Length; i++)
            {
                indexMap[i] = -1;
            }
            _cullingResults.SetLightIndexMap(indexMap);
            indexMap.Dispose();
            Shader.EnableKeyword(_lightsPerObjectKeyword);
        }
        else
        {
            Shader.DisableKeyword(_lightsPerObjectKeyword);
        }
        
        //  Setup to Shader
        _buffer.SetGlobalInt(_dirLightCountId, dirLightCount);
        if (dirLightCount > 0)
        {
            _buffer.SetGlobalVectorArray(_dirLightColorsId, _dirLightColors);
            _buffer.SetGlobalVectorArray(_dirLightDirectionsId, _dirLightDirections);
            _buffer.SetGlobalVectorArray(_dirLightShadowDataId, _dirLightShadowData);
        }
        
        _buffer.SetGlobalInt(_otherLightCountId, otherLightCount);
        if (otherLightCount > 0)
        {
            _buffer.SetGlobalVectorArray(_otherLightColorsId, _otherLightColors);
            _buffer.SetGlobalVectorArray(_otherLightPositionsId, _otherLightPositions);
            _buffer.SetGlobalVectorArray(_otherLightDirectionsId, _otherLightDirections);
            _buffer.SetGlobalVectorArray(_otherLightSpotAnglesId, _otherLightSpotAngles);
            _buffer.SetGlobalVectorArray(_otherLightShadowDataId, _otherLightShadowData);
        }
    }

    public void CleanUp()
    {
        _shadows.CleanUp();
    }
}
