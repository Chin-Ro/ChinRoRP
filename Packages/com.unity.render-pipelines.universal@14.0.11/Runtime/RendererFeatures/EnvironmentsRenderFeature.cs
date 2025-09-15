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
        public bool volumetricLighting = true;
        
        [Range(1, 1024)]
        public int maxLocalVolumetricFogOnScreen = 64;
        
        private RTHandle m_MaxZMask;
        private RTHandle m_VolumetricDensityBuffer;
        private RTHandle m_VolumetricLighting;
        private RTHandle m_LightShafts;
        private RTHandle m_LightShaftsBloom;

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
        
        const int k_VolumetricMaterialIndirectArgumentCount = 5;
        const int k_VolumetricMaterialIndirectArgumentByteSize = k_VolumetricMaterialIndirectArgumentCount * sizeof(uint);
        const int k_VolumetricFogPriorityMaxValue = 1048576; // 2^20 because there are 20 bits in the volumetric fog sort key
        
        ComputeBuffer m_VisibleVolumeBoundsBuffer = null;
        GraphicsBuffer m_VolumetricMaterialDataBuffer = null;
        GraphicsBuffer m_VolumetricMaterialIndexBuffer = null;
        Material m_DefaultVolumetricFogMaterial = null;
        
        private ShaderVariablesEnvironments m_ShaderVariablesEnvironment;
        private ShaderVariablesVolumetric m_ShaderVariablesVolumetric;
        public static int frameIndex => _frameIndex;
        private static int _frameIndex;

        private bool volumetricHistoryIsValid = false;
        private bool volumetricInit = false;

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
                if (!volumetricInit) InitializeVolumetricLighting();
                m_GenerateMaxZPass ??= new GenerateMaxZPass(environmentsData);
                m_ClearAndHeightFogVoxelizationPass ??= new ClearAndHeightFogVoxelizationPass(environmentsData);
                m_FogVolumeVoxelizationPass ??= new FogVolumeVoxelizationPass(environmentsData);
                m_VolumetricLightingPass ??= new VolumetricLightingPass();
                m_ShaderVariablesVolumetric = new ShaderVariablesVolumetric();
            }

            if (lightShafts)
            {
                m_LightShaftsPass ??= new LightShaftsPass(RenderPassEvent.BeforeRenderingSkybox);
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
                _frameIndex = (_frameIndex + 1) % 14;

                if (vBufferParams == null)
                {
                    var parameters = VolumetricLightingUtils.ComputeVolumetricBufferParameters(cameraData);
                    VolumetricLightingUtils.s_CurrentVolumetricBufferSize = parameters.viewportSize;
                    vBufferParams = new VBufferParameters[2];
                    vBufferParams[0] = parameters;
                    vBufferParams[1] = parameters;
                }
                else
                {
                    VolumetricLightingUtils.UpdateVolumetricBufferParams(cameraData, ref vBufferParams);
                }
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
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

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            // Frustum cull Local Volumetric Fog on the CPU. Can be performed as soon as the camera is set up.
            PrepareVisibleLocalVolumetricFogList(renderingData.cameraData, renderingData.commandBuffer);
            
            EnvironmentsUtils.UpdateShaderVariablesEnvironmentsCB(ref m_ShaderVariablesEnvironment, renderingData, vBufferParams);

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
                
                RenderPassEvent volumetricPassEvent = RenderPassEvent.AfterRenderingOpaques;
                bool isRendererDeferred = renderer is UniversalRenderer { renderingModeRequested: RenderingMode.Deferred };
                if (isRendererDeferred) volumetricPassEvent = RenderPassEvent.AfterRenderingGbuffer;
                
                m_GenerateMaxZPass.Setup(ref m_MaxZMask, volumetricPassEvent, isRendererDeferred, vBufferParams);
                
                descriptor.width = VolumetricLightingUtils.s_CurrentVolumetricBufferSize.x;
                descriptor.height = VolumetricLightingUtils.s_CurrentVolumetricBufferSize.y;
                descriptor.volumeDepth = VolumetricLightingUtils.s_CurrentVolumetricBufferSize.z;
                descriptor.dimension = TextureDimension.Tex3D;
                descriptor.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
                descriptor.enableRandomWrite = true;
                RenderingUtils.ReAllocateIfNeeded(ref m_VolumetricDensityBuffer, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "Volumetric Density");
                
                var currIdx = (frameIndex + 0) & 1;
                var currParams = vBufferParams[currIdx]; 
                var cvp = currParams.viewportSize;
                var resolution = new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y);
                VolumetricLightingUtils.UpdateShaderVariableslVolumetrics(ref m_ShaderVariablesVolumetric, renderingData.cameraData, resolution,
                    m_VisibleLocalVolumetricFogVolumes.Count, volumetricHistoryIsValid, vBufferParams);
                
                m_ClearAndHeightFogVoxelizationPass.Setup(ref m_VolumetricDensityBuffer, volumetricPassEvent, vBufferParams, m_ShaderVariablesVolumetric);
                
                m_FogVolumeVoxelizationPass.Setup(ref m_VolumetricDensityBuffer, volumetricPassEvent);
            }
            
            m_OpaqueAtmosphereScatteringPass.Setup(m_ShaderVariablesEnvironment);
        }

        protected override void Dispose(bool disposing)
        {
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
            m_MaxZMask?.Release();
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
        
        static int UnpackFogVolumeIndex(uint sortKey)
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
                        // if (m_VisibleLocalVolumetricFogVolumes.Count >= maxLocalVolumetricFogOnScreen)
                        // {
                        //     Debug.LogError($"The number of local volumetric fog in the view is above the limit: {m_VisibleLocalVolumetricFogVolumes.Count} instead of {maxLocalVolumetricFogOnScreen}. To fix this, please increase the maximum number of local volumetric fog in the view in the HDRP asset.");
                        //     break;
                        // }

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
    
    internal static class EnvironmentConstants
    {
        // CBuffers
        public static readonly int _ShaderVariablesEnvironments = Shader.PropertyToID("ShaderVariablesEnvironments");
        public static readonly int _ShaderVariablesVolumetric = Shader.PropertyToID("ShaderVariablesVolumetric");
        
        // Generate MaxZ
        public static readonly int _InputTexture = Shader.PropertyToID("_InputTexture");
        public static readonly int _OutputTexture = Shader.PropertyToID("_OutputTexture");
        public static readonly int _SrcOffsetAndLimit = Shader.PropertyToID("_SrcOffsetAndLimit");
        public static readonly int _DilationWidth = Shader.PropertyToID("_DilationWidth");
        
        // Volumetric Lighting
        public static readonly int _VBufferDensity = Shader.PropertyToID("_VBufferDensity");
        public static readonly int _VBufferLighting = Shader.PropertyToID("_VBufferLighting");
        public static readonly int _VBufferLightingFiltered = Shader.PropertyToID("_VBufferLightingFiltered");
        public static readonly int _VBufferHistory = Shader.PropertyToID("_VBufferHistory");
        public static readonly int _VBufferFeedback = Shader.PropertyToID("_VBufferFeedback");
        public static readonly int _VolumeBounds = Shader.PropertyToID("_VolumeBounds");
        public static readonly int _VolumeData = Shader.PropertyToID("_VolumeData");
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
        static void UpdateShaderVariablesGlobalVolumetrics(ref ShaderVariablesEnvironments cb, CameraData cameraData, in VBufferParameters[] vBufferParams)
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
            cb._VBufferLightingViewportScale = currParams.ComputeViewportScale(VolumetricLightingUtils.s_CurrentVolumetricBufferSize);
            cb._VBufferLightingViewportLimit = currParams.ComputeViewportLimit(VolumetricLightingUtils.s_CurrentVolumetricBufferSize);
            cb._VBufferDistanceEncodingParams = currParams.depthEncodingParams;
            cb._VBufferDistanceDecodingParams = currParams.depthDecodingParams;
            cb._VBufferLastSliceDist = currParams.ComputeLastSliceDistance(sliceCount);
            cb._VBufferRcpInstancedViewCount = 1.0f / 1;
        }
        internal static void UpdateShaderVariablesEnvironmentsCB(ref ShaderVariablesEnvironments cb, RenderingData renderingData, VBufferParameters[] vBufferParams)
        {
            var universalCamera = renderingData.cameraData;
            bool isMainLightingExists = renderingData.lightData.mainLightIndex >= 0;
            var fogSettings = VolumeManager.instance.stack.GetComponent<Fog>();
            
            fogSettings.UpdateShaderVariablesEnvironmentsCBFogParameters(ref cb, universalCamera, isMainLightingExists);

            UpdateShaderVariablesGlobalVolumetrics(ref cb, universalCamera, vBufferParams);
        }
    }
}
