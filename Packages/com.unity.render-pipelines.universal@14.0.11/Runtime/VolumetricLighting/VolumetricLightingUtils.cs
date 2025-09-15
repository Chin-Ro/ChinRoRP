//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  体积光照数学库
//--------------------------------------------------------------------------------------------------------

using System;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.Universal
{
    //--------------------------------------------VolumetricLightingUtils------------------------------------------
    #region VolumetricLightingUtils
    public class VolumetricLightingUtils
    {
        // This size is shared between all cameras to create the volumetric 3D textures
        internal static Vector3Int s_CurrentVolumetricBufferSize;
        
        private static void ComputeVolumetricFogSliceCountAndScreenFraction(Fog fog, out int sliceCount, out float screenFraction)
        {
            if (fog.fogControlMode == Fog.FogControl.Balance)
            {
                // Evaluate the ssFraction and sliceCount based on the control parameters
                float maxScreenSpaceFraction = (1.0f - fog.resolutionDepthRatio.value) * (Fog.maxFogScreenResolutionPercentage - Fog.minFogScreenResolutionPercentage) + Fog.minFogScreenResolutionPercentage;
                screenFraction = Mathf.Lerp(Fog.minFogScreenResolutionPercentage, maxScreenSpaceFraction, fog.volumetricFogBudget.value) * 0.01f;
                float maxSliceCount = Mathf.Max(1.0f, fog.resolutionDepthRatio.value * Fog.maxFogSliceCount);
                sliceCount = (int)Mathf.Lerp(1.0f, maxSliceCount, fog.volumetricFogBudget.value);
            }
            else
            {
                screenFraction = fog.screenResolutionPercentage.value * 0.01f;
                sliceCount = fog.volumeSliceCount.value;
            }
        }

        private static Vector3Int ComputeVolumetricViewportSize(CameraData universalCamera, ref float voxelSize)
        {
            var controller = VolumeManager.instance.stack.GetComponent<Fog>();
            Debug.Assert(controller != null);

            int viewportWidth = universalCamera.scaledWidth;
            int viewportHeight = universalCamera.scaledHeight;

            ComputeVolumetricFogSliceCountAndScreenFraction(controller, out var sliceCount, out var screenFraction);
            if (controller.fogControlMode == Fog.FogControl.Balance)
            {
                // Evaluate the voxel size
                voxelSize = 1.0f / screenFraction;
            }
            else
            {
                if (controller.screenResolutionPercentage.value == Fog.optimalFogScreenResolutionPercentage)
                    voxelSize = 8;
                else
                    voxelSize = 1.0f / screenFraction; // Does not account for rounding (same function, above)
            }

            int w = Mathf.RoundToInt(viewportWidth * screenFraction);
            int h = Mathf.RoundToInt(viewportHeight * screenFraction);
            int d = sliceCount;

            return new Vector3Int(w, h, d);
        }
        
        internal static VBufferParameters ComputeVolumetricBufferParameters(CameraData universalCamera)
        {
            var controller = VolumeManager.instance.stack.GetComponent<Fog>();
            Debug.Assert(controller != null);

            float voxelSize = 0;
            Vector3Int viewportSize = ComputeVolumetricViewportSize(universalCamera, ref voxelSize);

            return new VBufferParameters(viewportSize, controller.depthExtent.value,
                universalCamera.camera.nearClipPlane,
                universalCamera.camera.farClipPlane,
                universalCamera.camera.fieldOfView,
                controller.sliceDistributionUniformity.value,
                voxelSize);
        }
        
        // This function relies on being called once per camera per frame.
        // The results are undefined otherwise.
        internal static void UpdateVolumetricBufferParams(CameraData universalCamera, ref VBufferParameters[] vBufferParams)
        {
            if (!Fog.IsVolumetricFogEnabled(universalCamera))
                return;

            Debug.Assert(vBufferParams != null);
            Debug.Assert(vBufferParams.Length == 2);

            var currentParams = ComputeVolumetricBufferParameters(universalCamera);

            int frameIndex = EnvironmentsRenderFeature.frameIndex;
            var currIdx = (frameIndex + 0) & 1;
            var prevIdx = (frameIndex + 1) & 1;

            vBufferParams[currIdx] = currentParams;

            // Handle case of first frame. When we are on the first frame, we reuse the value of original frame.
            if (vBufferParams[prevIdx].viewportSize.x == 0.0f && vBufferParams[prevIdx].viewportSize.y == 0.0f)
            {
                vBufferParams[prevIdx] = currentParams;
            }

            // Update size used to create volumetric buffers.
            // s_CurrentVolumetricBufferSize = new Vector3Int(Math.Max(s_CurrentVolumetricBufferSize.x, currentParams.viewportSize.x),
            //     Math.Max(s_CurrentVolumetricBufferSize.y, currentParams.viewportSize.y),
            //     Math.Max(s_CurrentVolumetricBufferSize.z, currentParams.viewportSize.z));

            s_CurrentVolumetricBufferSize = currentParams.viewportSize;
        }
        
        public static float MeanFreePathFromExtinction(float extinction)
        {
            return 1.0f / extinction;
        }

        public static float ExtinctionFromMeanFreePath(float meanFreePath)
        {
            return 1.0f / meanFreePath;
        }

        public static Vector3 AbsorptionFromExtinctionAndScattering(float extinction, Vector3 scattering)
        {
            return new Vector3(extinction, extinction, extinction) - scattering;
        }

        public static Vector3 ScatteringFromExtinctionAndAlbedo(float extinction, Vector3 albedo)
        {
            return extinction * albedo;
        }

        public static Vector3 AlbedoFromMeanFreePathAndScattering(float meanFreePath, Vector3 scattering)
        {
            return meanFreePath * scattering;
        }
        
        public static float ScaleHeightFromLayerDepth(float d)
        {
            // Exp[-d / H] = 0.001
            // -d / H = Log[0.001]
            // H = d / -Log[0.001]
            return d * 0.144765f;
        }
        
        static float CornetteShanksPhasePartConstant(float anisotropy)
        {
            float g = anisotropy;

            return (3.0f / (8.0f * Mathf.PI)) * (1.0f - g * g) / (2.0f + g * g);
        }
        
        private static bool IsVolumetricReprojectionEnabled(CameraData camera)
        {
            bool a = Fog.IsVolumetricFogEnabled(camera);
            // We only enable volumetric re projection if we are processing the game view or a scene view with animated materials on
            bool b = camera.cameraType == CameraType.Game || (camera.cameraType == CameraType.SceneView && CoreUtils.AreAnimatedMaterialsEnabled(camera.camera));

            return a && b;
        }
        
        // Ref: https://en.wikipedia.org/wiki/Close-packing_of_equal_spheres
        // The returned {x, y} coordinates (and all spheres) are all within the (-0.5, 0.5)^2 range.
        // The pattern has been rotated by 15 degrees to maximize the resolution along X and Y:
        // https://www.desmos.com/calculator/kcpfvltz7c
        static void GetHexagonalClosePackedSpheres7(Vector2[] coords)
        {
            float r = 0.17054068870105443882f;
            float d = 2 * r;
            float s = r * Mathf.Sqrt(3);

            // Try to keep the weighted average as close to the center (0.5) as possible.
            //  (7)(5)    ( )( )    ( )( )    ( )( )    ( )( )    ( )(o)    ( )(x)    (o)(x)    (x)(x)
            // (2)(1)(3) ( )(o)( ) (o)(x)( ) (x)(x)(o) (x)(x)(x) (x)(x)(x) (x)(x)(x) (x)(x)(x) (x)(x)(x)
            //  (4)(6)    ( )( )    ( )( )    ( )( )    (o)( )    (x)( )    (x)(o)    (x)(x)    (x)(x)
            coords[0] = new Vector2(0, 0);
            coords[1] = new Vector2(-d, 0);
            coords[2] = new Vector2(d, 0);
            coords[3] = new Vector2(-r, -s);
            coords[4] = new Vector2(r, s);
            coords[5] = new Vector2(r, -s);
            coords[6] = new Vector2(-r, s);

            // Rotate the sampling pattern by 15 degrees.
            const float cos15 = 0.96592582628906828675f;
            const float sin15 = 0.25881904510252076235f;

            for (int i = 0; i < 7; i++)
            {
                Vector2 coord = coords[i];

                coords[i].x = coord.x * cos15 - coord.y * sin15;
                coords[i].y = coord.x * sin15 + coord.y * cos15;
            }
        }
        
        // This is a sequence of 7 equidistant numbers from 1/14 to 13/14.
        // Each of them is the centroid of the interval of length 2/14.
        // They've been rearranged in a sequence of pairs {small, large}, s.t. (small + large) = 1.
        // That way, the running average position is close to 0.5.
        // | 6 | 2 | 4 | 1 | 5 | 3 | 7 |
        // |   |   |   | o |   |   |   |
        // |   | o |   | x |   |   |   |
        // |   | x |   | x |   | o |   |
        // |   | x | o | x |   | x |   |
        // |   | x | x | x | o | x |   |
        // | o | x | x | x | x | x |   |
        // | x | x | x | x | x | x | o |
        // | x | x | x | x | x | x | x |
        static float[] m_zSeq = { 7.0f / 14.0f, 3.0f / 14.0f, 11.0f / 14.0f, 5.0f / 14.0f, 9.0f / 14.0f, 1.0f / 14.0f, 13.0f / 14.0f };
        static Vector2[] m_xySeq = new Vector2[7];
        
        internal static void UpdateShaderVariableslVolumetrics(ref ShaderVariablesVolumetric cb, CameraData universalCamera, in Vector4 resolution, int m_VisibleLocalVolumetricFogVolumesCount, bool volumetricHistoryIsValid, VBufferParameters[] vBufferParams)
        {
            var fog = VolumeManager.instance.stack.GetComponent<Fog>();
            var vFoV = universalCamera.camera.GetGateFittedFieldOfView() * Mathf.Deg2Rad;
            int frameIndex = EnvironmentsRenderFeature.frameIndex;
            
            cb._VBufferUnitDepthTexelSpacing = UniversalUtils.ComputZPlaneTexelSpacing(1.0f, vFoV, resolution.y);
            cb._NumVisibleLocalVolumetricFog = (uint)m_VisibleLocalVolumetricFogVolumesCount;
            cb._CornetteShanksConstant = CornetteShanksPhasePartConstant(fog.anisotropy.value);
            cb._VBufferHistoryIsValid = volumetricHistoryIsValid ? 1u : 0u;

            GetHexagonalClosePackedSpheres7(m_xySeq);
            int sampleIndex = EnvironmentsRenderFeature.frameIndex % 7;
            Vector4 xySeqOffset = new Vector4();
            // TODO: should we somehow reorder offsets in Z based on the offset in XY? S.t. the samples more evenly cover the domain.
            // Currently, we assume that they are completely uncorrelated, but maybe we should correlate them somehow.
            xySeqOffset.Set(m_xySeq[sampleIndex].x, m_xySeq[sampleIndex].y, m_zSeq[sampleIndex], frameIndex);
            cb._VBufferSampleOffset = xySeqOffset;
            
            var currIdx = (frameIndex + 0) & 1;
            var prevIdx = (frameIndex + 1) & 1;

            var currParams = vBufferParams[currIdx];
            var prevParams = vBufferParams[prevIdx];

            var pvp = prevParams.viewportSize;

            // The lighting & density buffers are shared by all cameras.
            // The history & feedback buffers are specific to the camera.
            // These 2 types of buffers can have different sizes.
            // Additionally, history buffers can have different sizes, since they are not resized at the same time.
            Vector3Int historyBufferSize = Vector3Int.zero;

            if (IsVolumetricReprojectionEnabled(universalCamera))
            {
                historyBufferSize = pvp;
            }

            cb._VBufferVoxelSize = currParams.voxelSize;
            cb._VBufferPrevViewportSize = new Vector4(pvp.x, pvp.y, 1.0f / pvp.x, 1.0f / pvp.y);
            cb._VBufferHistoryViewportScale = prevParams.ComputeViewportScale(historyBufferSize);
            cb._VBufferHistoryViewportLimit = prevParams.ComputeViewportLimit(historyBufferSize);
            cb._VBufferPrevDistanceEncodingParams = prevParams.depthEncodingParams;
            cb._VBufferPrevDistanceDecodingParams = prevParams.depthDecodingParams;
        }
        
        // https://iquilezles.org/www/articles/distfunctions/distfunctions.htm
        static float DistanceToOriginAABB(Vector3 point, Vector3 aabbSize)
        {
            float3 q = abs(point) - float3(aabbSize);
            return length(max(q, 0.0f)) + min(max(q.x, max(q.y, q.z)), 0.0f);
        }
        
        // Optimized version of https://www.sciencedirect.com/topics/computer-science/oriented-bounding-box
        internal static float DistanceToOBB(OrientedBBox obb, Vector3 point)
        {
            float3 offset = point - obb.center;
            float3 boxForward = normalize(cross(obb.right, obb.up));
            float3 axisAlignedPoint = float3(dot(offset, normalize(obb.right)), dot(offset, normalize(obb.up)), dot(offset, boxForward));

            return DistanceToOriginAABB(axisAlignedPoint, float3(obb.extentX, obb.extentY, obb.extentZ));
        }
    }
    #endregion
    
    //--------------------------------------------VBufferParameters------------------------------------------------
    #region VBufferParameters
    internal struct VBufferParameters
    {
        public Vector3Int viewportSize;
        public float voxelSize;
        public Vector4 depthEncodingParams;
        public Vector4 depthDecodingParams;

        public VBufferParameters(Vector3Int viewportSize, float depthExtent, float camNear, float camFar, float camVFoV,
                                 float sliceDistributionUniformity, float voxelSize)
        {
            this.viewportSize = viewportSize;
            this.voxelSize = voxelSize;

            // The V-Buffer is sphere-capped, while the camera frustum is not.
            // We always start from the near plane of the camera.

            float aspectRatio = viewportSize.x / (float)viewportSize.y;
            float farPlaneHeight = 2.0f * Mathf.Tan(0.5f * camVFoV) * camFar;
            float farPlaneWidth = farPlaneHeight * aspectRatio;
            float farPlaneMaxDim = Mathf.Max(farPlaneWidth, farPlaneHeight);
            float farPlaneDist = Mathf.Sqrt(camFar * camFar + 0.25f * farPlaneMaxDim * farPlaneMaxDim);

            float nearDist = camNear;
            float farDist = Math.Min(nearDist + depthExtent, farPlaneDist);

            float c = 2 - 2 * sliceDistributionUniformity; // remap [0, 1] -> [2, 0]
            c = Mathf.Max(c, 0.001f);                // Avoid NaNs

            depthEncodingParams = ComputeLogarithmicDepthEncodingParams(nearDist, farDist, c);
            depthDecodingParams = ComputeLogarithmicDepthDecodingParams(nearDist, farDist, c);
        }

        internal Vector3 ComputeViewportScale(Vector3Int bufferSize)
        {
            return new Vector3(UniversalUtils.ComputeViewportScale(viewportSize.x, bufferSize.x),
                UniversalUtils.ComputeViewportScale(viewportSize.y, bufferSize.y),
                UniversalUtils.ComputeViewportScale(viewportSize.z, bufferSize.z));
        }

        internal Vector3 ComputeViewportLimit(Vector3Int bufferSize)
        {
            return new Vector3(UniversalUtils.ComputeViewportLimit(viewportSize.x, bufferSize.x),
                UniversalUtils.ComputeViewportLimit(viewportSize.y, bufferSize.y),
                UniversalUtils.ComputeViewportLimit(viewportSize.z, bufferSize.z));
        }

        internal float ComputeLastSliceDistance(uint sliceCount)
        {
            float d = 1.0f - 0.5f / sliceCount;
            float ln2 = 0.69314718f;

            // DecodeLogarithmicDepthGeneralized(1 - 0.5 / sliceCount)
            return depthDecodingParams.x * Mathf.Exp(ln2 * d * depthDecodingParams.y) + depthDecodingParams.z;
        }

        float EncodeLogarithmicDepthGeneralized(float z, Vector4 encodingParams)
        {
            return encodingParams.x + encodingParams.y * Mathf.Log(Mathf.Max(0, z - encodingParams.z), 2);
        }

        float DecodeLogarithmicDepthGeneralized(float d, Vector4 decodingParams)
        {
            return decodingParams.x * Mathf.Pow(2, d * decodingParams.y) + decodingParams.z;
        }

        internal int ComputeSliceIndexFromDistance(float distance, int maxSliceCount)
        {
            // Avoid out of bounds access
            distance = Mathf.Clamp(distance, 0f, ComputeLastSliceDistance((uint)maxSliceCount));

            float vBufferNearPlane = DecodeLogarithmicDepthGeneralized(0, depthDecodingParams);

            // float dt = (distance - vBufferNearPlane) * 2;
            float dt = distance + vBufferNearPlane;
            float e1 = EncodeLogarithmicDepthGeneralized(dt, depthEncodingParams);
            float rcpSliceCount = 1.0f / (float)maxSliceCount;

            float slice = (e1 - rcpSliceCount) / rcpSliceCount;

            return (int)slice;
        }

        // See EncodeLogarithmicDepthGeneralized().
        static Vector4 ComputeLogarithmicDepthEncodingParams(float nearPlane, float farPlane, float c)
        {
            Vector4 depthParams = new Vector4();

            float n = nearPlane;
            float f = farPlane;

            depthParams.y = 1.0f / Mathf.Log(c * (f - n) + 1, 2);
            depthParams.x = Mathf.Log(c, 2) * depthParams.y;
            depthParams.z = n - 1.0f / c; // Same
            depthParams.w = 0.0f;

            return depthParams;
        }

        // See DecodeLogarithmicDepthGeneralized().
        static Vector4 ComputeLogarithmicDepthDecodingParams(float nearPlane, float farPlane, float c)
        {
            Vector4 depthParams = new Vector4();

            float n = nearPlane;
            float f = farPlane;

            depthParams.x = 1.0f / c;
            depthParams.y = Mathf.Log(c * (f - n) + 1, 2);
            depthParams.z = n - 1.0f / c; // Same
            depthParams.w = 0.0f;

            return depthParams;
        }
    }
    #endregion

    //--------------------------------------------LocalVolumetricFog-----------------------------------------------
    #region LocalVolumetricFog
    struct LocalVolumetricFogEngineData
    {
        public Vector3 scattering;    // [0, 1]
        public float extinction;    // [0, 1]
        public Vector3 textureTiling;
        public int invertFade;    // bool...
        public Vector3 textureScroll;
        public float rcpDistFadeLen;
        public Vector3 rcpPosFaceFade;
        public float endTimesRcpDistFadeLen;
        public Vector3 rcpNegFaceFade;
        public LocalVolumetricFogBlendingMode blendingMode;
        public Vector3 albedo;
        public LocalVolumetricFogFalloffMode falloffMode;

        public static LocalVolumetricFogEngineData GetNeutralValues()
        {
            LocalVolumetricFogEngineData data;

            data.scattering = Vector3.zero;
            data.extinction = 0;
            data.textureTiling = Vector3.one;
            data.textureScroll = Vector3.zero;
            data.rcpPosFaceFade = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            data.rcpNegFaceFade = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            data.invertFade = 0;
            data.rcpDistFadeLen = 0;
            data.endTimesRcpDistFadeLen = 1;
            data.falloffMode = LocalVolumetricFogFalloffMode.Linear;
            data.blendingMode = LocalVolumetricFogBlendingMode.Additive;
            data.albedo = Vector3.zero;

            return data;
        }
    } // struct VolumeProperties
    
    public enum LocalVolumetricFogFalloffMode
    {
        /// <summary>Fade using a linear function.</summary>
        Linear,
        /// <summary>Fade using an exponential function.</summary>
        Exponential,
    }

    /// <summary>Local volumetric fog blending mode.</summary>
    public enum LocalVolumetricFogBlendingMode
    {
        /// <summary>Replace the current fog, it is similar to disabling the blending.</summary>
        Overwrite   = 0,
        /// <summary>Additively blend fog volumes. This is the default behavior.</summary>
        Additive    = 1,
        /// <summary>Multiply the fog values when doing the blending. This is useful to make the fog density relative to other fog volumes.</summary>
        Multiply    = 2,
        /// <summary>Performs a minimum operation when blending the volumes.</summary>
        Min         = 3,
        /// <summary>Performs a maximum operation when blending the volumes.</summary>
        Max         = 4,
    }
    
    /// <summary>Select which mask mode to use for the local volumetric fog.</summary>
    public enum LocalVolumetricFogMaskMode
    {
        /// <summary>Use a 3D texture as mask.</summary>
        Texture,
        /// <summary>Use a material as mask. The material must use the "Fog Volume" material type in Shader Graph.</summary>
        Material,
    }
    #endregion
    
    struct OrientedBBox
    {
        // 3 x float4 = 48 bytes.
        // TODO: pack the axes into 16-bit UNORM per channel, and consider a quaternionic representation.
        public Vector3 right;
        public float extentX;
        public Vector3 up;
        public float extentY;
        public Vector3 center;
        public float extentZ;

        public Vector3 forward { get { return Vector3.Cross(up, right); } }

        public OrientedBBox(Matrix4x4 trs)
        {
            Vector3 vecX = trs.GetColumn(0);
            Vector3 vecY = trs.GetColumn(1);
            Vector3 vecZ = trs.GetColumn(2);

            center = trs.GetColumn(3);
            right = vecX * (1.0f / vecX.magnitude);
            up = vecY * (1.0f / vecY.magnitude);

            extentX = 0.5f * vecX.magnitude;
            extentY = 0.5f * vecY.magnitude;
            extentZ = 0.5f * vecZ.magnitude;
        }
    } // struct OrientedBBox
    
    struct ShaderVariablesVolumetric
    {
        public float _VBufferUnitDepthTexelSpacing;
        public uint _NumVisibleLocalVolumetricFog;
        public float _CornetteShanksConstant;
        public uint _VBufferHistoryIsValid;

        public Vector4 _VBufferSampleOffset;

        public float _VBufferVoxelSize;
        public float _HaveToPad;
        public float _OtherwiseTheBuffer;
        public float _IsFilledWithGarbage;
        public Vector4 _VBufferPrevViewportSize;
        public Vector4 _VBufferHistoryViewportScale;
        public Vector4 _VBufferHistoryViewportLimit;
        public Vector4 _VBufferPrevDistanceEncodingParams;
        public Vector4 _VBufferPrevDistanceDecodingParams;
    }
    
    struct VolumetricMaterialRenderingData
    {
        public Vector4 viewSpaceBounds;
        public uint startSliceIndex;
        public uint sliceCount;
        public uint padding0;
        public uint padding1;
    }
}