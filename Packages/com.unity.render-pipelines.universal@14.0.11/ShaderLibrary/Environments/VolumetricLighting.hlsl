#ifndef VOLUMETRIC_LIGHTING_INCLUDED
#define VOLUMETRIC_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
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
#endif
