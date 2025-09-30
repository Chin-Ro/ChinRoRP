//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  环境渲染特性：雾效、光柱、体积雾
//--------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
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
        public lightShaftsDownsampleMode lightShaftsDownsample = lightShaftsDownsampleMode.Half;
        
        [Min(0)] public int lightShaftBlurNumSamples = 12;
        [Min(0.0f)] public float lightShaftFirstPassDistance = 0.1f;
        
        public bool volumetricLighting = true;
        [Range(1, k_MaxVisibleLocalVolumetricFogCount)] public int maxLocalVolumetricFogOnScreen = 64;

        [Range(0f, 1f)] public float sliceDistributionUniformity = 0.75f;
        internal static float m_SliceDistributionUniformity;

        public FogDenoisingMode denoisingMode = FogDenoisingMode.Both;
        internal static FogDenoisingMode m_DenoisingMode;
        
        public FogControl fogControl = FogControl.Balance;
        internal static FogControl m_FogControl;
        
        [Range(1.0f / 16.0f * 100, 0.5f * 100)] public float screenResolutionPercentage = (1.0f / 8.0f) * 100;
        internal static float m_ScreenResolutionPercentage;

        [Range(1, 512)] public int volumeSliceCount = 64;
        internal static int m_VolumeSliceCount;

        [Range(0f, 1f)] public float volumetricFogBudget = 0.5f;
        internal static float m_FolumetricFogBudget;

        [Range(0f, 1f)] public float resolutionDepthRatio = 0.5f;
        internal static float m_ResolutionDepthRatio;
        
        private RTHandle m_MaxZMask;
        private RTHandle m_VolumetricDensityBuffer;
        private RTHandle m_VolumetricLighting;

        private GenerateMaxZPass m_GenerateMaxZPass;
        private ClearAndHeightFogVoxelizationPass m_ClearAndHeightFogVoxelizationPass;
        private FogVolumeVoxelizationPass m_FogVolumeVoxelizationPass;
        private VolumetricLightingPass m_VolumetricLightingPass;
        private LightShaftsPass m_LightShaftsPass;
        private OpaqueAtmosphereScatteringPass m_OpaqueAtmosphereScatteringPass;
        private LightShaftsBloomPass m_LightShaftsBloomPass;
        
        List<OrientedBBox> m_VisibleVolumeBounds = null;
        List<LocalVolumetricFogEngineData> m_VisibleVolumeData = null;
        List<LocalVolumetricFog> m_VisibleLocalVolumetricFogVolumes = null;
        NativeArray<uint> m_VolumetricFogSortKeys;
        NativeArray<uint> m_VolumetricFogSortKeysTemp;
        private const int k_MaxVisibleLocalVolumetricFogCount = 1024;
        
        const int k_VolumetricFogPriorityMaxValue = 1048576; // 2^20 because there are 20 bits in the volumetric fog sort key
        
        ComputeBuffer m_VisibleVolumeBoundsBuffer = null;
        GraphicsBuffer m_VolumetricMaterialDataBuffer = null;
        GraphicsBuffer m_VolumetricMaterialIndexBuffer = null;
        Material m_DefaultVolumetricFogMaterial = null;
        
        private ShaderVariablesEnvironments m_ShaderVariablesEnvironments;
        private ShaderVariablesVolumetric m_ShaderVariablesVolumetric;
        public static int frameIndex => _frameIndex;
        private static int _frameIndex;
        
        private Vector3Int s_CurrentVolumetricBufferSize;
        
        private bool volumetricInit = false;

        private VBufferParameters[] vBufferParams;
        
        private RenderTextureDescriptor descriptor = new RenderTextureDescriptor();

        public enum lightShaftsDownsampleMode
        {
            Full = 0,
            Half = 1,
            Quarter = 2,
            Octant = 3
        }
        
        public override void Create()
        {
            m_DenoisingMode = denoisingMode;
            m_SliceDistributionUniformity = sliceDistributionUniformity;
            m_FogControl = fogControl;
            m_ScreenResolutionPercentage = screenResolutionPercentage;
            m_VolumeSliceCount = volumeSliceCount;
            m_FolumetricFogBudget = volumetricFogBudget;
            m_ResolutionDepthRatio = resolutionDepthRatio;
            
#if UNITY_EDITOR
            if (environmentsData == null)
            {
                environmentsData =
                    AssetDatabase.LoadAssetAtPath<EnvironmentsData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/EnvironmentsData.asset");
            }
#endif
            
            if (volumetricLighting)
            {
                if (!volumetricInit) InitializeVolumetricLighting();
                m_GenerateMaxZPass ??= new GenerateMaxZPass(environmentsData);
                m_ClearAndHeightFogVoxelizationPass ??= new ClearAndHeightFogVoxelizationPass(environmentsData);
                m_FogVolumeVoxelizationPass ??= new FogVolumeVoxelizationPass(environmentsData);
                m_VolumetricLightingPass ??= new VolumetricLightingPass(environmentsData);
                m_ShaderVariablesVolumetric = new ShaderVariablesVolumetric();
            }

            if (lightShafts)
            {
                m_LightShaftsPass ??= new LightShaftsPass(RenderPassEvent.BeforeRenderingSkybox, environmentsData);
                m_LightShaftsBloomPass ??= new LightShaftsBloomPass(RenderPassEvent.BeforeRenderingPostProcessing, environmentsData);
            }

            m_ShaderVariablesEnvironments = new ShaderVariablesEnvironments();
            m_OpaqueAtmosphereScatteringPass ??= new OpaqueAtmosphereScatteringPass(RenderPassEvent.AfterRenderingSkybox, environmentsData);
        }

        public override void OnCameraPreCull(ScriptableRenderer renderer, in CameraData cameraData)
        {
            _frameIndex = (_frameIndex + 1) % 14;

            if (vBufferParams == null)
            {
                var parameters = VolumetricLightingUtils.ComputeVolumetricBufferParameters(cameraData);
                s_CurrentVolumetricBufferSize = parameters.viewportSize;
                vBufferParams = new VBufferParameters[2];
                vBufferParams[0] = parameters;
                vBufferParams[1] = parameters;
            }
            else
            {
                VolumetricLightingUtils.UpdateVolumetricBufferParams(cameraData, ref vBufferParams, ref s_CurrentVolumetricBufferSize);
            }
            
            bool enableFog = Fog.IsFogEnabled(cameraData);
            if (enableFog)
            {
                bool enableVolumetricFog = volumetricLighting && Fog.IsVolumetricFogEnabled(cameraData);
                if (enableVolumetricFog)
                {
                    bool isVolumetricHistoryRequired = Fog.IsVolumetricReprojectionEnabled(cameraData);
                    // Handle the volumetric fog buffers
                    if (isVolumetricHistoryRequired)
                    {
                        if (cameraData.volumetricHistoryBuffers == null)
                        {
                            VolumetricLightingUtils.CreateVolumetricHistoryBuffers(cameraData, 2);
                            cameraData.isFirstFrame = false;
                        }
                    }
                    else
                    {
                        VolumetricLightingUtils.DestroyVolumetricHistoryBuffers(cameraData);
                        cameraData.isFirstFrame = true;
                    }
                    VolumetricLightingUtils.ResizeVolumetricHistoryBuffers(cameraData, vBufferParams);
                }
                else
                {
                    VolumetricLightingUtils.DestroyVolumetricHistoryBuffers(cameraData);
                    cameraData.isFirstFrame = true;
                }
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            bool enableFog = Fog.IsFogEnabled(renderingData.cameraData);
            
            if (enableFog)
            {
                bool enableVolumetricFog = volumetricLighting && Fog.IsVolumetricFogEnabled(renderingData.cameraData);
                if (enableVolumetricFog)
                {
                    renderer.EnqueuePass(m_GenerateMaxZPass);
                    renderer.EnqueuePass(m_ClearAndHeightFogVoxelizationPass);
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
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            bool enableFog = Fog.IsFogEnabled(renderingData.cameraData);
            bool enableVolumetricFog = volumetricLighting && Fog.IsVolumetricFogEnabled(renderingData.cameraData);
            bool enableLightShafts = renderingData.lightData.mainLightIndex >= 0 && lightShafts && Fog.IsLightShaftsEnabled(renderingData.cameraData);
            
            EnvironmentsUtils.UpdateShaderVariablesEnvironmentsCB(ref m_ShaderVariablesEnvironments, renderingData, vBufferParams, s_CurrentVolumetricBufferSize, enableLightShafts, enableVolumetricFog);
            ConstantBuffer.PushGlobal(renderingData.commandBuffer, m_ShaderVariablesEnvironments, EnvironmentConstants._ShaderVariablesEnvironments);
            
            if (enableFog)
            {
                if (enableVolumetricFog)
                {
                    // Frustum cull Local Volumetric Fog on the CPU. Can be performed as soon as the camera is set up.
                    PrepareVisibleLocalVolumetricFogList(renderingData.cameraData, renderingData.commandBuffer);

                    descriptor.width = (int)(renderingData.cameraData.scaledWidth / 16.0f);
                    descriptor.height = (int)(renderingData.cameraData.scaledHeight / 16.0f);
                    descriptor.graphicsFormat = GraphicsFormat.R32_SFloat;
                    descriptor.dimension = TextureDimension.Tex2D;
                    descriptor.enableRandomWrite = true;
                    descriptor.msaaSamples = 1;
                    RenderingUtils.ReAllocateIfNeeded(ref m_MaxZMask, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "Dilated MaxZ mask");
                    
                    RenderPassEvent volumetricPassEvent = RenderPassEvent.AfterRenderingOpaques;
                    bool isRendererDeferred = renderer is UniversalRenderer { renderingModeRequested: RenderingMode.Deferred };
                    if (isRendererDeferred) volumetricPassEvent = RenderPassEvent.AfterRenderingGbuffer;
                    
                    m_GenerateMaxZPass.Setup(ref m_MaxZMask, volumetricPassEvent, isRendererDeferred, vBufferParams);
                    
                    descriptor.width = s_CurrentVolumetricBufferSize.x;
                    descriptor.height = s_CurrentVolumetricBufferSize.y;
                    descriptor.volumeDepth = s_CurrentVolumetricBufferSize.z;
                    descriptor.dimension = TextureDimension.Tex3D;
                    descriptor.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
                    descriptor.enableRandomWrite = true;
                    RenderingUtils.ReAllocateIfNeeded(ref m_VolumetricDensityBuffer, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "Volumetric Density");
                    
                    var currIdx = (frameIndex + 0) & 1;
                    var currParams = vBufferParams[currIdx];
                    var cvp = currParams.viewportSize;
                    var resolution = new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y);
                    VolumetricLightingUtils.UpdateShaderVariableslVolumetrics(ref m_ShaderVariablesVolumetric, renderingData.cameraData, resolution,
                        m_VisibleLocalVolumetricFogVolumes.Count, vBufferParams);
                    
                    m_ClearAndHeightFogVoxelizationPass.Setup(ref m_VolumetricDensityBuffer, volumetricPassEvent, vBufferParams, m_ShaderVariablesVolumetric);

                    m_FogVolumeVoxelizationPass.Setup(ref m_VolumetricDensityBuffer, volumetricPassEvent, m_VisibleLocalVolumetricFogVolumes,
                        maxLocalVolumetricFogOnScreen, vBufferParams, m_VisibleVolumeBoundsBuffer, m_VolumetricMaterialDataBuffer, m_DefaultVolumetricFogMaterial,
                        m_VisibleVolumeData, m_VisibleVolumeBounds, m_VolumetricMaterialIndexBuffer, m_VolumetricFogSortKeys);

                    RenderingUtils.ReAllocateIfNeeded(ref m_VolumetricLighting, descriptor,  FilterMode.Bilinear, TextureWrapMode.Clamp, name: "VBufferLighting");

                    m_VolumetricLightingPass.Setup(ref m_VolumetricLighting, m_VolumetricDensityBuffer, m_MaxZMask, volumetricPassEvent, vBufferParams,
                        m_ShaderVariablesVolumetric);
                }

                if (enableLightShafts)
                {
                    descriptor.width = Mathf.Max(1, renderingData.cameraData.scaledWidth >> (int)lightShaftsDownsample);
                    descriptor.height = Mathf.Max(1, renderingData.cameraData.scaledHeight >> (int)lightShaftsDownsample);
                    descriptor.volumeDepth = 1;
                    descriptor.dimension = TextureDimension.Tex2D;
                    descriptor.graphicsFormat = GraphicsFormat.R8_UNorm;
                    descriptor.enableRandomWrite = false;
                    descriptor.msaaSamples = 1;
                    
                    m_LightShaftsPass.Setup(descriptor, lightShaftBlurNumSamples, lightShaftFirstPassDistance);
                }
                
                if (enableLightShafts && Fog.IsLightShaftsBloomEnabled(renderingData.cameraData))
                {
                    descriptor.graphicsFormat = renderingData.cameraData.renderer.cameraColorTargetHandle.rt.graphicsFormat;
                    m_LightShaftsBloomPass.Setup(descriptor, lightShaftBlurNumSamples, lightShaftFirstPassDistance);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            m_MaxZMask?.Release();
            m_VolumetricDensityBuffer?.Release();
            m_VolumetricLighting?.Release();
            CleanupVolumetricLighting();
            m_GenerateMaxZPass?.Dispose();
            m_GenerateMaxZPass = null;
            m_ClearAndHeightFogVoxelizationPass?.Dispose();
            m_ClearAndHeightFogVoxelizationPass = null;
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
            _frameIndex = 0;
            vBufferParams = null;
        }
        
        void InitializeVolumetricLighting()
        {
            m_VisibleVolumeBounds = new List<OrientedBBox>(); 
            m_VisibleVolumeData = new List<LocalVolumetricFogEngineData>();
            m_VisibleLocalVolumetricFogVolumes = new List<LocalVolumetricFog>();
            m_VisibleVolumeBoundsBuffer = new ComputeBuffer(k_MaxVisibleLocalVolumetricFogCount, Marshal.SizeOf(typeof(OrientedBBox)));
            int maxVolumeCountTimesViewCount = k_MaxVisibleLocalVolumetricFogCount * 2;
            m_VolumetricMaterialDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxVolumeCountTimesViewCount, Marshal.SizeOf(typeof(VolumetricMaterialRenderingData)));
            m_VolumetricMaterialIndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, 3 * 4, sizeof(uint));
            m_VolumetricFogSortKeys = new NativeArray<uint>(maxLocalVolumetricFogOnScreen, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_VolumetricFogSortKeysTemp = new NativeArray<uint>(maxLocalVolumetricFogOnScreen, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            // Index buffer for triangle fan with max 6 vertices
            m_VolumetricMaterialIndexBuffer.SetData(new List<uint>{
                0, 1, 2,
                0, 2, 3,
                0, 3, 4,
                0, 4, 5
            });
            m_DefaultVolumetricFogMaterial = CoreUtils.CreateEngineMaterial(environmentsData.defaultFogVolumeShader);
            volumetricInit = true;
        }
        
        void CleanupVolumetricLighting()
        {
            CoreUtils.SafeRelease(m_VisibleVolumeBoundsBuffer);
            CoreUtils.SafeRelease(m_VolumetricMaterialIndexBuffer);
            CoreUtils.SafeRelease(m_VolumetricMaterialDataBuffer);
            CoreUtils.Destroy(m_DefaultVolumetricFogMaterial);

            if (m_VolumetricFogSortKeys.IsCreated)
                m_VolumetricFogSortKeys.Dispose();
            if (m_VolumetricFogSortKeysTemp.IsCreated)
                m_VolumetricFogSortKeysTemp.Dispose();

            m_VisibleVolumeData = null; // free()
            m_VisibleVolumeBounds = null; // free()
            m_VisibleLocalVolumetricFogVolumes = null;
            m_DefaultVolumetricFogMaterial = null;
            volumetricInit = false;
        }
        
        uint PackFogVolumeSortKey(LocalVolumetricFog fog, int index)
        {
            // 12 bit index, 20 bit priority
            int halfMaxPriority = k_VolumetricFogPriorityMaxValue / 2;
            int clampedPriority = Mathf.Clamp(fog.parameters.priority, -halfMaxPriority, halfMaxPriority) + halfMaxPriority;
            uint priority = (uint)(clampedPriority & 0xFFFFF);
            uint fogIndex = (uint)(index & 0xFFF);
            return (priority << 12) | (fogIndex << 0);
        }
        
        internal static int UnpackFogVolumeIndex(uint sortKey)
        {
            return (int)(sortKey & 0xFFF);
        }

        void PrepareVisibleLocalVolumetricFogList(CameraData universalCamera, CommandBuffer cmd)
        {
            if (!Fog.IsVolumetricFogEnabled(universalCamera))
                return;

            using (new ProfilingScope(cmd, new ProfilingSampler("PrepareVisibleLocalVolumetricFogList")))
            {
                Vector3 camPosition = universalCamera.camera.transform.position;
                Vector3 camOffset = Vector3.zero;// World-origin-relative
                m_VisibleVolumeBounds.Clear();
                m_VisibleVolumeData.Clear();
                m_VisibleLocalVolumetricFogVolumes.Clear();

                // Collect all visible finite volume data, and upload it to the GPU.
                var volumes = LocalVolumetricFogManager.manager.PrepareLocalVolumetricFogData(cmd, universalCamera);
                var fog = VolumeManager.instance.stack.GetComponent<Fog>();

                foreach (var volume in volumes)
                {
                    var obb = new OrientedBBox(Matrix4x4.TRS(volume.transform.position, volume.transform.rotation, volume.parameters.size));

                    // Reject volumes that are completely fade out or outside of the volumetric fog
                    float minObbDistance = VolumetricLightingUtils.DistanceToOBB(obb, camPosition) - universalCamera.camera.nearClipPlane;
                    if (minObbDistance > volume.parameters.distanceFadeEnd || minObbDistance > fog.depthExtent.value)
                        continue;

                    // Handle camera-relative rendering.
                    obb.center -= camOffset;

                    // Frustum cull on the CPU for now. TODO: do it on the GPU.
                    // TODO: account for custom near and far planes of the V-Buffer's frustum.
                    // It's typically much shorter (along the Z axis) than the camera's frustum.
                    //if (GeometryUtils.Overlap(obb, hdCamera.frustum, 6, 8))
                    {
                        if (m_VisibleLocalVolumetricFogVolumes.Count >= maxLocalVolumetricFogOnScreen)
                        {
                            Debug.LogError($"The number of local volumetric fog in the view is above the limit: {m_VisibleLocalVolumetricFogVolumes.Count} instead of {maxLocalVolumetricFogOnScreen}. To fix this, please increase the maximum number of local volumetric fog in the view in the HDRP asset.");
                            break;
                        }

                        // TODO: cache these?
                        m_VisibleVolumeBounds.Add(obb);
                        m_VisibleVolumeData.Add(volume.parameters.ConvertToEngineData());

                        m_VisibleLocalVolumetricFogVolumes.Add(volume);
                    }
                }

                // Assign priorities for sorting
                for (int i = 0; i < m_VisibleLocalVolumetricFogVolumes.Count; i++)
                    m_VolumetricFogSortKeys[i] = PackFogVolumeSortKey(m_VisibleLocalVolumetricFogVolumes[i], i);

                // Stable sort to avoid flickering
                CoreUnsafeUtils.MergeSort(m_VolumetricFogSortKeys, m_VisibleLocalVolumetricFogVolumes.Count, ref m_VolumetricFogSortKeysTemp);

                m_VisibleVolumeBoundsBuffer.SetData(m_VisibleVolumeBounds);
            }
        }
    }
    
    public enum FogControl
    {
        /// <summary>
        /// Use this mode if you want to change the fog control properties based on a higher abstraction level centered around performance.
        /// </summary>
        Balance,

        /// <summary>
        /// Use this mode if you want to have direct access to the internal properties that control volumetric fog.
        /// </summary>
        Manual
    }
    
    public enum FogDenoisingMode
    {
        None = 0,
        Reprojection = 1 << 0,
        Gaussian = 1 << 1,
        Both = Reprojection | Gaussian
    }
    
    internal static class EnvironmentConstants
    {
        // CBuffers
        public static readonly int _ShaderVariablesEnvironments = Shader.PropertyToID("ShaderVariablesEnvironments");
        public static readonly int _ShaderVariablesVolumetric = Shader.PropertyToID("ShaderVariablesVolumetric");
        
        // Generate MaxZ
        public static readonly int _InputTexture = Shader.PropertyToID("_InputTexture");
        public static readonly int _OutputTexture = Shader.PropertyToID("_OutputTexture");
        public static readonly int _SrcOffsetAndLimit = Shader.PropertyToID("_SrcOffsetAndLimit");
        public static readonly int _MaxZMaskTexture = Shader.PropertyToID("_MaxZMaskTexture");
        public static readonly int _DilationWidth = Shader.PropertyToID("_DilationWidth");
        
        // Volumetric Lighting
        public static readonly int _VBufferDensity = Shader.PropertyToID("_VBufferDensity");
        public static readonly int _VBufferLighting = Shader.PropertyToID("_VBufferLighting");
        public static readonly int _VBufferHistory = Shader.PropertyToID("_VBufferHistory");
        public static readonly int _VBufferFeedback = Shader.PropertyToID("_VBufferFeedback");
        public static readonly int _VolumeBounds = Shader.PropertyToID("_VolumeBounds");
        
        // Volumetric Materials
        public static readonly int _VolumeCount = Shader.PropertyToID("_VolumeCount");
        public static readonly int _VolumeMaterialDataIndex = Shader.PropertyToID("_VolumeMaterialDataIndex");
        public static readonly int _CameraRight = Shader.PropertyToID("_CameraRight");
        public static readonly int _MaxSliceCount = Shader.PropertyToID("_MaxSliceCount");
        public static readonly int _VolumetricIndirectBufferArguments = Shader.PropertyToID("_IndirectBufferArguments");
        public static readonly int _VolumetricMaterialData = Shader.PropertyToID("_VolumetricMaterialData");
        public static readonly int _VolumetricMask = Shader.PropertyToID("_Mask");
        public static readonly int _VolumetricScrollSpeed = Shader.PropertyToID("_ScrollSpeed");
        public static readonly int _VolumetricTiling = Shader.PropertyToID("_Tiling");
        public static readonly int _VolumetricViewIndex = Shader.PropertyToID("_ViewIndex");
        public static readonly int _VolumetricViewCount = Shader.PropertyToID("_ViewCount");
        public static readonly int _CameraInverseViewProjection_NO = Shader.PropertyToID("_CameraInverseViewProjection_NO");
        public static readonly int _IsObliqueProjectionMatrix = Shader.PropertyToID("_IsObliqueProjectionMatrix");
        public static readonly int _VolumetricMaterialDataCBuffer = Shader.PropertyToID("VolumetricMaterialDataCBuffer");
        
        // 3D Atlas
        public static readonly int _Dst3DTexture = Shader.PropertyToID("_Dst3DTexture");
        public static readonly int _Src3DTexture = Shader.PropertyToID("_Src3DTexture");
        public static readonly int _AlphaOnlyTexture = Shader.PropertyToID("_AlphaOnlyTexture");
        public static readonly int _SrcSize = Shader.PropertyToID("_SrcSize");
        public static readonly int _SrcMip = Shader.PropertyToID("_SrcMip");
        public static readonly int _SrcScale = Shader.PropertyToID("_SrcScale");
        public static readonly int _SrcOffset = Shader.PropertyToID("_SrcOffset");
        
        // Light Shafts
        public static readonly int _DirectionalLightScreenPos = Shader.PropertyToID("_DirectionalLightScreenPos");
        public static readonly int _LightShaftParameters = Shader.PropertyToID("_LightShaftParameters");
        public static readonly int _RadialBlurParameters = Shader.PropertyToID("_RadialBlurParameters");
        public static readonly int _LightShaftTexture = Shader.PropertyToID("_LightShaftTexture");
        public static readonly int _LightShaftsBloomTexture = Shader.PropertyToID("_LightShaftsBloomTexture");
        public static readonly int _BloomMaxBrightness = Shader.PropertyToID("_BloomMaxBrightness");
        public static readonly int _BloomTintAndThreshold = Shader.PropertyToID("_BloomTintAndThreshold");
    }
    
    internal struct ShaderVariablesEnvironments
    {
        // Volumetric lighting / Fog.
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
        
        // VBuffer
        public Vector4 _VBufferViewportSize;           // { w, h, 1/w, 1/h }
        public Vector4 _VBufferLightingViewportScale;  // Necessary us to work with sub-allocation (resource aliasing) in the RTHandle system
        public Vector4 _VBufferLightingViewportLimit;  // Necessary us to work with sub-allocation (resource aliasing) in the RTHandle system
        public Vector4 _VBufferDistanceEncodingParams; // See the call site for description
        public Vector4 _VBufferDistanceDecodingParams; // See the call site for description
        public uint _VBufferSliceCount;
        public float _VBufferRcpSliceCount;
        public float _VBufferRcpInstancedViewCount;  // Used to remap VBuffer coordinates for XR
        public float _VBufferLastSliceDist;          // The distance to the middle of the last slice
    }
    
    internal static class EnvironmentsUtils
    {
        static void UpdateShaderVariablesGlobalVolumetrics(ref ShaderVariablesEnvironments cb, CameraData cameraData, in VBufferParameters[] vBufferParams, in Vector3Int s_CurrentVolumetricBufferSize)
        {
            if (!Fog.IsVolumetricFogEnabled(cameraData))
            {
                return;
            }
            
            uint frameIndex = (uint)EnvironmentsRenderFeature.frameIndex;
            uint currIdx = (frameIndex + 0) & 1;

            var currParams = vBufferParams[currIdx];

            // The lighting & density buffers are shared by all cameras.
            // The history & feedback buffers are specific to the camera.
            // These 2 types of buffers can have different sizes.
            // Additionally, history buffers can have different sizes, since they are not resized at the same time.
            var cvp = currParams.viewportSize;

            // Adjust slices for XR rendering: VBuffer is shared for all single-pass views
            uint sliceCount = (uint)(cvp.z / 1);

            cb._VBufferViewportSize = new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y);
            cb._VBufferSliceCount = sliceCount;
            cb._VBufferRcpSliceCount = 1.0f / sliceCount;
            cb._VBufferLightingViewportScale = currParams.ComputeViewportScale(s_CurrentVolumetricBufferSize);
            cb._VBufferLightingViewportLimit = currParams.ComputeViewportLimit(s_CurrentVolumetricBufferSize);
            cb._VBufferDistanceEncodingParams = currParams.depthEncodingParams;
            cb._VBufferDistanceDecodingParams = currParams.depthDecodingParams;
            cb._VBufferLastSliceDist = currParams.ComputeLastSliceDistance(sliceCount);
            cb._VBufferRcpInstancedViewCount = 1.0f / 1;
        }
        internal static void UpdateShaderVariablesEnvironmentsCB(ref ShaderVariablesEnvironments cb, RenderingData renderingData, VBufferParameters[] vBufferParams, Vector3Int s_CurrentVolumetricBufferSize, bool enableLightShafts, bool enableVolumetricFog)
        {
            var universalCamera = renderingData.cameraData;
            bool isMainLightingExists = renderingData.lightData.mainLightIndex >= 0;
            var fogSettings = VolumeManager.instance.stack.GetComponent<Fog>();
            
            fogSettings.UpdateShaderVariablesEnvironmentsCBFogParameters(ref cb, universalCamera, isMainLightingExists, enableLightShafts, enableVolumetricFog);

            UpdateShaderVariablesGlobalVolumetrics(ref cb, universalCamera, vBufferParams, s_CurrentVolumetricBufferSize);
        }
    }
}
