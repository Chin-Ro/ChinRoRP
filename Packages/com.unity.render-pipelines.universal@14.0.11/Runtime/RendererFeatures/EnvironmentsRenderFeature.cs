//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  环境渲染特性：雾效、光柱、体积雾
//--------------------------------------------------------------------------------------------------------

using System;
using UnityEngine.Experimental.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.Universal
{
    [DisallowMultipleRendererFeature("Environments")]
    public class EnvironmentsRenderFeature : ScriptableRendererFeature
    {
        public EnvironmentsData environmentsData;
        public bool lightShafts = true;
        public bool volumetricLighting = true;
        
        private RTHandle m_MaxZMask;
        private RTHandle m_VolumetricDensityBuffer;
        private RTHandle m_VolumetricLighting;
        private RTHandle m_LightShafts;
        private RTHandle m_LightShaftsBloom;

        private GenerateMaxZPass m_GenerateMaxZPass;
        private FogVolumeVoxelizationPass m_FogVolumeVoxelizationPass;
        private VolumetricLightingPass m_VolumetricLightingPass;
        private LightShaftsPass m_LightShaftsPass;
        private OpaqueAtmosphereScatteringPass m_OpaqueAtmosphereScatteringPass;
        private LightShaftsBloomPass m_LightShaftsBloomPass;
        
        private ShaderVariablesEnvironments m_ShaderVariablesEnvironment;
        public static int frameCount => _frameCount;
        private static int _frameCount;

        private VBufferParameters[] vBufferParams;
        
        public override void Create()
        {
#if UNITY_EDITOR
            if (environmentsData == null)
            {
                environmentsData =
                    AssetDatabase.LoadAssetAtPath<EnvironmentsData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/EnvironmentsData.asset");
            }
#endif
            if (volumetricLighting)
            {
                m_GenerateMaxZPass ??= new GenerateMaxZPass(RenderPassEvent.AfterRenderingPrePasses, environmentsData);
                m_FogVolumeVoxelizationPass ??= new FogVolumeVoxelizationPass(RenderPassEvent.AfterRenderingPrePasses);
                m_VolumetricLightingPass ??= new VolumetricLightingPass(RenderPassEvent.AfterRenderingShadows);
            }

            if (lightShafts)
            {
                m_LightShaftsPass ??= new LightShaftsPass(RenderPassEvent.AfterRenderingPrePasses);
                m_LightShaftsBloomPass ??= new LightShaftsBloomPass(RenderPassEvent.BeforeRenderingPostProcessing);
            }

            m_ShaderVariablesEnvironment = new ShaderVariablesEnvironments();
            m_OpaqueAtmosphereScatteringPass ??= new OpaqueAtmosphereScatteringPass(RenderPassEvent.AfterRenderingSkybox, environmentsData);
        }

        public override void OnCameraPreCull(ScriptableRenderer renderer, in CameraData cameraData)
        {
            bool enableVolumetricFog = volumetricLighting && Fog.IsVolumetricFogEnabled(cameraData);
            if (enableVolumetricFog)
            {
                _frameCount = (_frameCount + 1) % 14;

                if (vBufferParams == null)
                {
                    var parameters = EnvironmentsUtils.ComputeVolumetricBufferParameters(cameraData);
                    vBufferParams = new VBufferParameters[2];
                    vBufferParams[0] = parameters;
                    vBufferParams[1] = parameters;
                }
                else
                {
                    EnvironmentsUtils.UpdateVolumetricBufferParams(cameraData, ref vBufferParams);
                }
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            bool enableVolumetricFog = volumetricLighting && Fog.IsVolumetricFogEnabled(renderingData.cameraData);
            if (enableVolumetricFog)
            {
                renderer.EnqueuePass(m_GenerateMaxZPass);
                renderer.EnqueuePass(m_FogVolumeVoxelizationPass);
                renderer.EnqueuePass(m_VolumetricLightingPass);
            }

            bool enableLightShafts = renderingData.lightData.mainLightIndex >= 0 && lightShafts && Fog.IsLightShaftsEnabled(renderingData.cameraData);
            if (enableLightShafts)
            {
                renderer.EnqueuePass(m_LightShaftsPass);
            }
            
            renderer.EnqueuePass(m_OpaqueAtmosphereScatteringPass);

            bool enableLightShaftsBloom = renderingData.lightData.mainLightIndex >= 0 && lightShafts && Fog.IsLightShaftsBloomEnabled(renderingData.cameraData);
            if (enableLightShaftsBloom)
            {
                renderer.EnqueuePass(m_LightShaftsBloomPass);
            }
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            EnvironmentsUtils.UpdateShaderVariablesEnvironmentsCB(ref m_ShaderVariablesEnvironment, renderingData);

            bool enableVolumetricFog = volumetricLighting && Fog.IsVolumetricFogEnabled(renderingData.cameraData);
            if (enableVolumetricFog)
            {
                RenderTextureDescriptor descriptor = new RenderTextureDescriptor((int)(renderingData.cameraData.scaledWidth / 16.0f),
                    (int)(renderingData.cameraData.scaledHeight / 16.0f))
                {
                    graphicsFormat = GraphicsFormat.R32_SFloat,
                    dimension = TextureDimension.Tex2D,
                    enableRandomWrite = true,
                    msaaSamples = 1,
                    depthBufferBits = 0,
                };
                RenderingUtils.ReAllocateIfNeeded(ref m_MaxZMask, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "Dilated MaxZ mask");
                m_GenerateMaxZPass.Setup(vBufferParams, ref m_MaxZMask);
            }
            
            m_OpaqueAtmosphereScatteringPass.Setup(m_ShaderVariablesEnvironment);
        }

        protected override void Dispose(bool disposing)
        {
            m_GenerateMaxZPass?.Dispose();
            m_GenerateMaxZPass = null;
            m_FogVolumeVoxelizationPass?.Dispose();
            m_FogVolumeVoxelizationPass = null;
            m_VolumetricLightingPass?.Dispose();
            m_VolumetricLightingPass = null;
            m_LightShaftsPass?.Dispose();
            m_LightShaftsPass = null;
            m_OpaqueAtmosphereScatteringPass?.Dispose();
            m_OpaqueAtmosphereScatteringPass = null;
            m_LightShaftsBloomPass?.Dispose();
            m_LightShaftsBloomPass = null;
            _frameCount = 0;
            vBufferParams = null;
            m_MaxZMask?.Release();
        }
    }
    
    //--------------------------------------------EnvironmentConstants---------------------------------------------
    internal static class EnvironmentConstants
    {
        // CBuffers
        public static readonly int _ShaderVariablesEnvironments = Shader.PropertyToID("ShaderVariablesEnvironments");
        
        // Generate MaxZ
        public static readonly int _InputTexture = Shader.PropertyToID("_InputTexture");
        public static readonly int _OutputTexture = Shader.PropertyToID("_OutputTexture");
        public static readonly int _SrcOffsetAndLimit = Shader.PropertyToID("_SrcOffsetAndLimit");
        public static readonly int _DilationWidth = Shader.PropertyToID("_DilationWidth");
    }
    
    //--------------------------------------------ShaderVariablesEnvironments--------------------------------------
    internal struct ShaderVariablesEnvironments
    {
        public Vector4 _ExponentialFogParameters;
        public Vector4 _ExponentialFogParameters2;
        public Vector4 _DirectionalInscatteringColor;
        public Vector4 _ExponentialFogParameters3;
        public Vector4 _ExponentialFogColorParameter;
        public Vector4 _MipFogParameters;
        public Vector4 _HeightFogBaseScattering;
        public Vector4 _HeightFogExponents;
        public Vector4 _GlobalFogParam1;
        public Vector4 _GlobalFogParam2;
    }
    
    //--------------------------------------------EnvironmentsUtils------------------------------------------------
    #region EnvironmentsUtils
    internal static class EnvironmentsUtils
    {
        // This size is shared between all cameras to create the volumetric 3D textures
        static Vector3Int s_CurrentVolumetricBufferSize;
        
        internal static void UpdateShaderVariablesEnvironmentsCB(ref ShaderVariablesEnvironments cb, RenderingData renderingData)
        {
            var universalCamera = renderingData.cameraData;
            bool isMainLightingExists = renderingData.lightData.mainLightIndex >= 0;
            var fogSettings = VolumeManager.instance.stack.GetComponent<Fog>();
            
            fogSettings.UpdateShaderVariablesEnvironmentsCBFogParameters(ref cb, universalCamera, isMainLightingExists);
        }
        
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

            int frameIndex = EnvironmentsRenderFeature.frameCount;
            var currIdx = (frameIndex + 0) & 1;
            var prevIdx = (frameIndex + 1) & 1;

            vBufferParams[currIdx] = currentParams;

            // Handle case of first frame. When we are on the first frame, we reuse the value of original frame.
            if (vBufferParams[prevIdx].viewportSize.x == 0.0f && vBufferParams[prevIdx].viewportSize.y == 0.0f)
            {
                vBufferParams[prevIdx] = currentParams;
            }

            // Update size used to create volumetric buffers.
            s_CurrentVolumetricBufferSize = new Vector3Int(Math.Max(s_CurrentVolumetricBufferSize.x, currentParams.viewportSize.x),
                Math.Max(s_CurrentVolumetricBufferSize.y, currentParams.viewportSize.y),
                Math.Max(s_CurrentVolumetricBufferSize.z, currentParams.viewportSize.z));
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

        private float ComputeLastSliceDistance(uint sliceCount)
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
}
