#ifndef VOLUMETRIC_LIGHTING_INCLUDED
#define VOLUMETRIC_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

//
// UnityEngine.Rendering.HighDefinition.LocalVolumetricFogBlendingMode:  static fields
//
#define LOCALVOLUMETRICFOGBLENDINGMODE_OVERWRITE (0)
#define LOCALVOLUMETRICFOGBLENDINGMODE_ADDITIVE (1)
#define LOCALVOLUMETRICFOGBLENDINGMODE_MULTIPLY (2)
#define LOCALVOLUMETRICFOGBLENDINGMODE_MIN (3)
#define LOCALVOLUMETRICFOGBLENDINGMODE_MAX (4)

//
// UnityEngine.Rendering.HighDefinition.LocalVolumetricFogFalloffMode:  static fields
//
#define LOCALVOLUMETRICFOGFALLOFFMODE_LINEAR (0)
#define LOCALVOLUMETRICFOGFALLOFFMODE_EXPONENTIAL (1)

CBUFFER_START(ShaderVariablesVolumetric)
float _VBufferUnitDepthTexelSpacing;
uint _NumVisibleLocalVolumetricFog;
float _CornetteShanksConstant;
uint _VBufferHistoryIsValid;
float4 _VBufferSampleOffset;
float _VBufferVoxelSize;
float _HaveToPad;
float _OtherwiseTheBuffer;
float _IsFilledWithGarbage;
float4 _VBufferPrevViewportSize;
float4 _VBufferHistoryViewportScale;
float4 _VBufferHistoryViewportLimit;
float4 _VBufferPrevDistanceEncodingParams;
float4 _VBufferPrevDistanceDecodingParams;
CBUFFER_END

struct OrientedBBox
{
    float3 right;
    float extentX;
    float3 up;
    float extentY;
    float3 center;
    float extentZ;
};

float3 GetRight(OrientedBBox value)
{
    return value.right;
}
float GetExtentX(OrientedBBox value)
{
    return value.extentX;
}
float3 GetUp(OrientedBBox value)
{
    return value.up;
}
float GetExtentY(OrientedBBox value)
{
    return value.extentY;
}
float3 GetCenter(OrientedBBox value)
{
    return value.center;
}
float GetExtentZ(OrientedBBox value)
{
    return value.extentZ;
}

// Generated from UnityEngine.Rendering.HighDefinition.VolumetricMaterialDataCBuffer
// PackingRules = Exact
CBUFFER_START(VolumetricMaterialDataCBuffer)
    float4 _VolumetricMaterialObbRight;
    float4 _VolumetricMaterialObbUp;
    float4 _VolumetricMaterialObbExtents;
    float4 _VolumetricMaterialObbCenter;
    float4 _VolumetricMaterialAlbedo;
    float4 _VolumetricMaterialRcpPosFaceFade;
    float4 _VolumetricMaterialRcpNegFaceFade;
    float _VolumetricMaterialInvertFade;
    float _VolumetricMaterialExtinction;
    float _VolumetricMaterialRcpDistFadeLen;
    float _VolumetricMaterialEndTimesRcpDistFadeLen;
    float _VolumetricMaterialFalloffMode;
    float padding0;
    float padding1;
    float padding2;
CBUFFER_END

struct LocalVolumetricFogEngineData
{
    float3 scattering;
    float extinction;
    float3 textureTiling;
    int invertFade;
    float3 textureScroll;
    float rcpDistFadeLen;
    float3 rcpPosFaceFade;
    float endTimesRcpDistFadeLen;
    float3 rcpNegFaceFade;
    int blendingMode;
    float3 albedo;
    int falloffMode;
};

// Generated from UnityEngine.Rendering.HighDefinition.VolumetricMaterialRenderingData
// PackingRules = Exact
struct VolumetricMaterialRenderingData
{
    float4 viewSpaceBounds;
    uint startSliceIndex;
    uint sliceCount;
    uint padding0;
    uint padding1;
    float4 obbVertexPositionWS[8];
};

//
// Accessors for UnityEngine.Rendering.HighDefinition.LocalVolumetricFogEngineData
//
float3 GetScattering(LocalVolumetricFogEngineData value)
{
    return value.scattering;
}
float GetExtinction(LocalVolumetricFogEngineData value)
{
    return value.extinction;
}
float3 GetTextureTiling(LocalVolumetricFogEngineData value)
{
    return value.textureTiling;
}
int GetInvertFade(LocalVolumetricFogEngineData value)
{
    return value.invertFade;
}
float3 GetTextureScroll(LocalVolumetricFogEngineData value)
{
    return value.textureScroll;
}
float GetRcpDistFadeLen(LocalVolumetricFogEngineData value)
{
    return value.rcpDistFadeLen;
}
float3 GetRcpPosFaceFade(LocalVolumetricFogEngineData value)
{
    return value.rcpPosFaceFade;
}
float GetEndTimesRcpDistFadeLen(LocalVolumetricFogEngineData value)
{
    return value.endTimesRcpDistFadeLen;
}
float3 GetRcpNegFaceFade(LocalVolumetricFogEngineData value)
{
    return value.rcpNegFaceFade;
}
int GetBlendingMode(LocalVolumetricFogEngineData value)
{
    return value.blendingMode;
}
float3 GetAlbedo(LocalVolumetricFogEngineData value)
{
    return value.albedo;
}
int GetFalloffMode(LocalVolumetricFogEngineData value)
{
    return value.falloffMode;
}
#endif
