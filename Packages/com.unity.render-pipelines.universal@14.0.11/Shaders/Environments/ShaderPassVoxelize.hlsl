#ifndef SHADERPASS_VOXELIZE_INCLUDED
#define SHADERPASS_VOXELIZE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Environments/VolumetricLighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Environments/ShaderVariablesEnvironments.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"

uint _VolumeMaterialDataIndex;
uint _ViewIndex;
float3 _CameraRight;
uint _IsObliqueProjectionMatrix;
float4x4 _CameraInverseViewProjection_NO;
StructuredBuffer<VolumetricMaterialRenderingData> _VolumetricMaterialData;

CBUFFER_START(UnityPerMaterial)
float4 _EmissionColor;
float _UseShadowThreshold;
float4 _DoubleSidedConstants;
float _BlendMode;
CBUFFER_END

// Jittered ray with screen-space derivatives.
struct JitteredRay
{
    float3 originWS;
    float3 centerDirWS;
    float3 jitterDirWS;
    float3 xDirDerivWS;
    float3 yDirDerivWS;
};

struct VertexToFragment
{
    float4 positionCS : SV_POSITION;
    float3 viewDirectionWS : TEXCOORD0;
    float3 positionOS : TEXCOORD1;
    uint depthSlice : SV_RenderTargetArrayIndex;
};

struct FragInputs
{
    // Contain value return by SV_POSITION (That is name positionCS in PackedVarying).
    // xy: unormalized screen position (offset by 0.5), z: device depth, w: depth in view space
    // Note: SV_POSITION is the result of the clip space position provide to the vertex shaders that is transform by the viewport
    float4 positionSS; // In case depth offset is use, positionRWS.w is equal to depth offset
    float3 positionRWS; // Relative camera space position
    float3 positionPredisplacementRWS; // Relative camera space position
    float2 positionPixel;              // Pixel position (VPOS)

    float4 texCoord0;

    // TODO: confirm with Morten following statement
    // Our TBN is orthogonal but is maybe not orthonormal in order to be compliant with external bakers (Like xnormal that use mikktspace).
    // (xnormal for example take into account the interpolation when baking the normal and normalizing the tangent basis could cause distortion).
    // When using tangentToWorld with surface gradient, it doesn't normalize the tangent/bitangent vector (We instead use exact same scale as applied to interpolated vertex normal to avoid breaking compliance).
    // this mean that any usage of tangentToWorld[1] or tangentToWorld[2] outside of the context of normal map (like for POM) must normalize the TBN (TCHECK if this make any difference ?)
    // When not using surface gradient, each vector of tangentToWorld are normalize (TODO: Maybe they should not even in case of no surface gradient ? Ask Morten)
    float3x3 tangentToWorld;
};

// Graph Pixel
struct SurfaceDescription
{
    float3 BaseColor;
    float Alpha;
};

float VBufferDistanceToSliceIndex(uint sliceIndex)
{
    float t0 = DecodeLogarithmicDepthGeneralized(0, _VBufferDistanceDecodingParams);
    float de = _VBufferRcpSliceCount; // Log-encoded distance between slices

    float e1 = ((float)sliceIndex + 0.5) * de + de;
    return DecodeLogarithmicDepthGeneralized(e1, _VBufferDistanceDecodingParams);
}

float EyeDepthToLinear(float linearDepth, float4 zBufferParam)
{
    linearDepth = rcp(linearDepth);
    linearDepth -= zBufferParam.w;

    return linearDepth / zBufferParam.z;
}

VertexToFragment Vert(uint instanceId : INSTANCEID_SEMANTIC, uint vertexId : VERTEXID_SEMANTIC)
{
    VertexToFragment output;

#if defined(UNITY_STEREO_INSTANCING_ENABLED)
    unity_StereoEyeIndex = _ViewIndex;
#endif

    uint sliceCount = _VolumetricMaterialData[_VolumeMaterialDataIndex].sliceCount;
    uint sliceStartIndex = _VolumetricMaterialData[_VolumeMaterialDataIndex].startSliceIndex;

    uint sliceIndex = sliceStartIndex + (instanceId % sliceCount);
    output.depthSlice = sliceIndex + _ViewIndex * _VBufferSliceCount;

    float sliceDepth = VBufferDistanceToSliceIndex(sliceIndex);

    output.positionCS = GetQuadVertexPosition(vertexId);
    output.positionCS.xy = output.positionCS.xy * _VolumetricMaterialData[_VolumeMaterialDataIndex].viewSpaceBounds.zw + _VolumetricMaterialData[_VolumeMaterialDataIndex].viewSpaceBounds.xy;
    output.positionCS.z = EyeDepthToLinear(sliceDepth, _ZBufferParams);
    output.positionCS.w = 1;

    float3 positionWS = ComputeWorldSpacePosition(output.positionCS, _IsObliqueProjectionMatrix ? _CameraInverseViewProjection_NO : UNITY_MATRIX_I_VP);
    output.viewDirectionWS = GetWorldSpaceViewDir(positionWS);

    // Calculate object space position
    output.positionOS = mul(UNITY_MATRIX_I_M, float4(positionWS, 1)).xyz;

    return output;
}

FragInputs BuildFragInputs(VertexToFragment v2f, float3 voxelPositionOS, float3 voxelClipSpace)
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    float3 positionWS = mul(UNITY_MATRIX_M, float4(voxelPositionOS, 1)).xyz;
    output.positionSS = v2f.positionCS;
    output.positionRWS = output.positionPredisplacementRWS = positionWS;
    output.positionPixel = uint2(v2f.positionCS.xy);
    output.texCoord0 = float4(saturate(voxelClipSpace * 0.5 + 0.5), 0);
    output.tangentToWorld = k_identity3x3;

    return output;
}

float ComputeFadeFactor(float3 coordNDC, float distance)
{
    bool exponential = uint(_VolumetricMaterialFalloffMode) == LOCALVOLUMETRICFOGFALLOFFMODE_EXPONENTIAL;

    return ComputeVolumeFadeFactor(
        coordNDC, distance,
        _VolumetricMaterialRcpPosFaceFade.xyz,
        _VolumetricMaterialRcpNegFaceFade.xyz,
        _VolumetricMaterialInvertFade,
        _VolumetricMaterialRcpDistFadeLen,
        _VolumetricMaterialEndTimesRcpDistFadeLen,
        exponential
    );
}

// --------------------------------------------------
// Build Surface Data

SurfaceDescription SurfaceDescriptionFunction()
{
    SurfaceDescription surface = (SurfaceDescription)0;
    surface.BaseColor = 0.5;
    surface.Alpha = float(1);
    return surface;
}
        
void GetVolumeData(FragInputs fragInputs, float3 V, out float3 scatteringColor, out float density)
{
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction();
        
    scatteringColor = surfaceDescription.BaseColor;
    density = surfaceDescription.Alpha;
}

void Frag(VertexToFragment v2f, out float4 outColor : SV_Target0)
{
    // Setup VR storeo eye index manually because we use the SV_RenderTargetArrayIndex semantic which conflicts with XR macros
#if defined(UNITY_SINGLE_PASS_STEREO)
    unity_StereoEyeIndex = _ViewIndex;
#endif

    float3 albedo;
    float extinction;

    float sliceDepth = VBufferDistanceToSliceIndex(v2f.depthSlice % _VBufferSliceCount);
    float3 cameraForward = -UNITY_MATRIX_V[2].xyz;
    float sliceDistance = sliceDepth;// / dot(-v2f.viewDirectionWS, cameraForward);

    // Compute voxel center position and test against volume OBB
    float3 raycenterDirWS = normalize(-v2f.viewDirectionWS); // Normalize
    float3 rayoriginWS    = GetCurrentViewPosition();
    float3 voxelCenterWS = rayoriginWS + sliceDistance * raycenterDirWS;

    float3x3 obbFrame = float3x3(_VolumetricMaterialObbRight.xyz, _VolumetricMaterialObbUp.xyz, cross(_VolumetricMaterialObbRight.xyz, _VolumetricMaterialObbUp.xyz));

    float3 voxelCenterBS = mul(voxelCenterWS - _VolumetricMaterialObbCenter.xyz, transpose(obbFrame));
    float3 voxelCenterCS = (voxelCenterBS * rcp(_VolumetricMaterialObbExtents.xyz));

    // Still need to clip pixels outside of the box because of the froxel buffer shape
    bool overlap = Max3(abs(voxelCenterCS.x), abs(voxelCenterCS.y), abs(voxelCenterCS.z)) <= 1;
    if (!overlap)
        clip(-1);

    FragInputs fragInputs = BuildFragInputs(v2f, voxelCenterBS, voxelCenterCS);
    GetVolumeData(fragInputs, v2f.viewDirectionWS, albedo, extinction);

    // Accumulate volume parameters
    extinction *= _VolumetricMaterialExtinction;
    albedo *= _VolumetricMaterialAlbedo.rgb;

    float3 voxelCenterNDC = saturate(voxelCenterCS * 0.5 + 0.5);
    float fade = ComputeFadeFactor(voxelCenterNDC, sliceDistance);

    // When multiplying fog, we need to handle specifically the blend area to avoid creating gaps in the fog
    #if defined FOG_VOLUME_BLENDING_MULTIPLY
    outColor = max(0, lerp(float4(1.0, 1.0, 1.0, 1.0), float4(saturate(albedo * extinction), extinction), fade.xxxx));
    #else
    extinction *= fade;
    outColor = max(0, float4(saturate(albedo * extinction), extinction));
    #endif
}
#endif