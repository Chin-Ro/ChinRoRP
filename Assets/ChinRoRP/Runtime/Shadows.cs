using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    private const string BufferName = "Shadows";

    private CommandBuffer _buffer = new CommandBuffer()
    {
        name = BufferName
    };

    private ScriptableRenderContext _context;

    private CullingResults _cullingResults;

    private ShadowSettings _shadowSettings;

    private const int MaxShadowedDirectionalLightCount = 4, MaxShadowedOtherLightCount = 16, MaxCascades = 4;

    struct ShadowedDirectionalLight
    {
        public int VisibleLightIndex;
        public float SlopeScaleBias;
        public float NearPlaneOffset;
    };

    private ShadowedDirectionalLight[] _shadowedDirectionalLights =
        new ShadowedDirectionalLight[MaxShadowedDirectionalLightCount];

    private struct ShadowedOtherLight
    {
        public int VisibleLightIndex;
        public float SlopeScaleBias;
        public float NormalBias;
    }

    private ShadowedOtherLight[] _shadowedOtherLights = new ShadowedOtherLight[MaxShadowedOtherLightCount];
    
    private int _shadowedDirectionalLightCount, _shadowedOtherLightCount;

    private static int
        _dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
        _dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices"),
        _otherShadowAtlasId = Shader.PropertyToID("_OtherShadowAtlas"),
        _otherShadowMatricesId = Shader.PropertyToID("_OtherShadowMatrices"),
        _cascadeCountId = Shader.PropertyToID("_CascadeCount"),
        _cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres"),
        _cascadeDataId = Shader.PropertyToID("_CascadeData"),
        _shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize"),
        //_shadowDistanceId = Shader.PropertyToID("_ShadowDistance");
        _shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");

    private static Vector4[]
        _cascadeCullingSpheres = new Vector4[MaxCascades],
        _cascadeData = new Vector4[MaxCascades];

    private Vector4 _atlasSizes;

    private static Matrix4x4[]
        _dirShadowMatrices = new Matrix4x4[MaxShadowedDirectionalLightCount * MaxCascades],
        _otherShadowMatrices = new Matrix4x4[MaxShadowedOtherLightCount];

    private static string[] _directionalFilterKeywords =
    {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7"
    };

    private static string[] _otherFilterKeywords =
    {
        "_OTHER_PCF3",
        "_OTHER_PCF5",
        "_OTHER_PCF7"
    };

    private static string[] _cascadeBlendKeywords =
    {
        "_CASCADE_BLEND_SOFT",
        "_CASCADE_BLEND_DITHER"
    };

    private static string[] _shadowMaskKeywords =
    {
        "_SHADOW_MASK_ALWAYS",
        "_SHADOW_MASK_DISTANCE"
    };

    private bool _useShadowMask;
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this._context = context;
        this._cullingResults = cullingResults;
        this._shadowSettings = shadowSettings;
        _shadowedDirectionalLightCount = _shadowedOtherLightCount = 0;
        _useShadowMask = false;
    }

    void ExecuteBuffer()
    {
        _context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }

    public Vector4 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (_shadowedDirectionalLightCount < MaxShadowedDirectionalLightCount && 
            light.shadows != LightShadows.None && light.shadowStrength > 0f /*&&
            _cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)*/)
        {
            float maskChannel = -1;
            LightBakingOutput lightBakingOutput = light.bakingOutput;
            if (lightBakingOutput.lightmapBakeType == LightmapBakeType.Mixed &&
                lightBakingOutput.mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                _useShadowMask = true;
                maskChannel = lightBakingOutput.occlusionMaskChannel;
            }

            if (!_cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
            {
                return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
            }
            _shadowedDirectionalLights[_shadowedDirectionalLightCount] = new ShadowedDirectionalLight
            {
                VisibleLightIndex = visibleLightIndex,
                SlopeScaleBias = light.shadowBias,
                NearPlaneOffset = light.shadowNearPlane
            };
            return new Vector4(light.shadowStrength, _shadowSettings.directional.cascadeCount * _shadowedDirectionalLightCount++, light.shadowNormalBias, maskChannel);
        }
        return new Vector4(0f, 0f, 0f, -1f);
    }

    public Vector4 ReserveOtherShadows(Light light, int visibleLightIndex)
    {
        if (light.shadows == LightShadows.None || light.shadowStrength <= 0f)
        {
            return new Vector4(0f, 0f, 0f, -1f);
        }

        float maskChannel = -1f;
        LightBakingOutput lightBakingOutput = light.bakingOutput;
        if (lightBakingOutput.lightmapBakeType == LightmapBakeType.Mixed &&
            lightBakingOutput.mixedLightingMode == MixedLightingMode.Shadowmask)
        {
            _useShadowMask = true;
            maskChannel = lightBakingOutput.occlusionMaskChannel;
        }

        if (_shadowedOtherLightCount >= MaxShadowedOtherLightCount ||
            !_cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
        }
        
        _shadowedOtherLights[_shadowedOtherLightCount] = new ShadowedOtherLight
        {
            VisibleLightIndex = visibleLightIndex,
            SlopeScaleBias = light.shadowBias,
            NormalBias = light.shadowNormalBias
        };
        
        return new Vector4(light.shadowStrength, _shadowedOtherLightCount++, 0f, maskChannel);
        //return new Vector4(0f, 0f, 0f, -1f);
    }
    public void Render()
    {
        if (_shadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            _buffer.GetTemporaryRT(_dirShadowAtlasId, 1, 1, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }

        if (_shadowedOtherLightCount > 0)
        {
            RenderOtherShadows();
        }
        else
        {
            _buffer.SetGlobalTexture(_otherShadowAtlasId, _dirShadowAtlasId);
        }
        _buffer.BeginSample(BufferName);
        SetKeywords(_shadowMaskKeywords, _useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);
        _buffer.SetGlobalInt(_cascadeCountId, _shadowedDirectionalLightCount > 0 ? _shadowSettings.directional.cascadeCount : 0);
        float f = 1f - _shadowSettings.directional.cascadeFade;
        _buffer.SetGlobalVector(_shadowDistanceFadeId, new Vector4(1f / _shadowSettings.maxDistance, 1f / _shadowSettings.distanceFade, 1f / (1f - f * f)));
        _buffer.SetGlobalVector(_shadowAtlasSizeId, _atlasSizes);
        _buffer.EndSample(BufferName);
        ExecuteBuffer();
    }

    void RenderDirectionalShadows()
    {
        int atlasSize = (int)_shadowSettings.directional.atlasSize;
        _atlasSizes.x = atlasSize;
        _atlasSizes.y = 1f / atlasSize;
        _buffer.GetTemporaryRT(_dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        _buffer.SetRenderTarget(_dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        _buffer.ClearRenderTarget(true, false, Color.clear);
        //_buffer.SetGlobalFloat(_shadowDistanceId, _shadowSettings.maxDistance);
        // float f = 1f - _shadowSettings.directional.cascadeFade;
        // _buffer.SetGlobalVector(_shadowDistanceFadeId, new Vector4(1f /_shadowSettings.maxDistance, 1f/ _shadowSettings.distanceFade, 1f / (1f - f * f)));
        _buffer.BeginSample(BufferName);
        ExecuteBuffer();

        int tiles = _shadowedDirectionalLightCount * _shadowSettings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;

        for (int i = 0; i < _shadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }
        //_buffer.SetGlobalInt(_cascadeCountId, _shadowSettings.directional.cascadeCount);
        _buffer.SetGlobalVectorArray(_cascadeCullingSpheresId, _cascadeCullingSpheres);
        _buffer.SetGlobalVectorArray(_cascadeDataId, _cascadeData);
        _buffer.SetGlobalMatrixArray(_dirShadowMatricesId, _dirShadowMatrices);
        SetKeywords(_directionalFilterKeywords, (int)_shadowSettings.directional.filter - 1);
        SetKeywords(_cascadeBlendKeywords, (int)_shadowSettings.directional.cascadeBlend - 1);
        //_buffer.SetGlobalVector(_shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize));
        _buffer.EndSample(BufferName);
        ExecuteBuffer();
    }
    
    void RenderOtherShadows()
    {
        int atlasSize = (int)_shadowSettings.other.atlasSize;
        _atlasSizes.z = atlasSize;
        _atlasSizes.w = 1f / atlasSize;
        _buffer.GetTemporaryRT(_otherShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        _buffer.SetRenderTarget(_otherShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        _buffer.ClearRenderTarget(true, false, Color.clear);
        //_buffer.SetGlobalFloat(_shadowDistanceId, _shadowSettings.maxDistance);
        // float f = 1f - _shadowSettings.directional.cascadeFade;
        // _buffer.SetGlobalVector(_shadowDistanceFadeId, new Vector4(1f /_shadowSettings.maxDistance, 1f/ _shadowSettings.distanceFade, 1f / (1f - f * f)));
        _buffer.BeginSample(BufferName);
        ExecuteBuffer();

        int tiles = _shadowedOtherLightCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;

        for (int i = 0; i < _shadowedOtherLightCount; i++)
        {
            RenderSpotShadows(i, split, tileSize);
        }
        
        _buffer.SetGlobalMatrixArray(_otherShadowMatricesId, _otherShadowMatrices);
        SetKeywords(_otherFilterKeywords, (int)_shadowSettings.other.filter - 1);
        _buffer.EndSample(BufferName);
        ExecuteBuffer();
    }
    void SetKeywords(string[] keywords, int enabledIndex)
    {
        //int enabledIndex = (int)_shadowSettings.directional.filter - 1;
        for (int i = 1; i < keywords.Length; i++)
        {
            if (i == enabledIndex)
            {
                _buffer.EnableShaderKeyword(keywords[i]);
            }
            else
            {
                _buffer.DisableShaderKeyword(keywords[i]);
            }
        }
    }

    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = _shadowedDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(_cullingResults, light.VisibleLightIndex);
        int cascadeCount = _shadowSettings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = _shadowSettings.directional.CascadeRatios;
        float cullingFactor = Mathf.Max(0f, 0.8f - _shadowSettings.directional.cascadeFade);

        for (int i = 0; i < cascadeCount; i++)
        {
            _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.VisibleLightIndex, i, cascadeCount,
                ratios, tileSize, light.NearPlaneOffset, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                out ShadowSplitData splitData);
            splitData.shadowCascadeBlendCullingFactor = cullingFactor;
            shadowSettings.splitData = splitData;
            if (index == 0)
            {
                SetCascadeData(i, splitData.cullingSphere, tileSize);
            }
            int tileIndex = tileOffset + i;
            //SetTileViewport(index, split, tileSize);
            _dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, SetTileViewport(tileIndex, split, tileSize), split);
            _buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            //_buffer.SetGlobalDepthBias(0f, 3f);
            _buffer.SetGlobalDepthBias(0f, light.SlopeScaleBias);
            ExecuteBuffer();
            _context.DrawShadows(ref shadowSettings);
            _buffer.SetGlobalDepthBias(0f, 0f);
            //_buffer.SetGlobalDepthBias(0f, 0f);
        }
    }

    void RenderSpotShadows(int index, int split, int tileSize)
    {
        ShadowedOtherLight light = _shadowedOtherLights[index];
        var shadowSettings = new ShadowDrawingSettings(_cullingResults, light.VisibleLightIndex);
        _cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(light.VisibleLightIndex, out Matrix4x4 viewMatrix,
            out Matrix4x4 projMatrix, out ShadowSplitData splitData);
        shadowSettings.splitData = splitData;
        _otherShadowMatrices[index] =
            ConvertToAtlasMatrix(projMatrix * viewMatrix, SetTileViewport(index, split, tileSize), split);
        _buffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
        _buffer.SetGlobalDepthBias(0f, light.SlopeScaleBias);
        ExecuteBuffer();
        _context.DrawShadows(ref shadowSettings);
        _buffer.SetGlobalDepthBias(0f, 0f);
    }
    
    void SetCascadeData(int index, Vector4 cullingSphere, int tileSize)
    {
        float texelSize = 2f * cullingSphere.w / tileSize;
        float filterSize = texelSize * ((float)_shadowSettings.directional.filter + 1f);
        //_cascadeData[index].x = 1f / cullingSphere.w;
        cullingSphere.w -= filterSize;
        cullingSphere.w *= cullingSphere.w;
        _cascadeCullingSpheres[index] = cullingSphere;
        _cascadeData[index] = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
    }
    public void CleanUp()
    {
        _buffer.ReleaseTemporaryRT(_dirShadowAtlasId);
        if (_shadowedOtherLightCount > 0)
        {
            _buffer.ReleaseTemporaryRT(_otherShadowAtlasId);
        }
        ExecuteBuffer();
    }

    Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        _buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }

    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }

        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        return m;
    }
}
