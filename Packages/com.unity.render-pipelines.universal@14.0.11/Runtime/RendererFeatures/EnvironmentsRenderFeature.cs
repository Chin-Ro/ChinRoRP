//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  环境渲染特性：雾效、光柱、体积雾
//--------------------------------------------------------------------------------------------------------

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
        private ClearAndHeightFogVoxelizationPass m_ClearAndHeightFogVoxelizationPass;
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
                m_GenerateMaxZPass ??= new GenerateMaxZPass(environmentsData);
                m_ClearAndHeightFogVoxelizationPass ??= new ClearAndHeightFogVoxelizationPass(environmentsData);
                m_FogVolumeVoxelizationPass ??= new FogVolumeVoxelizationPass();
                m_VolumetricLightingPass ??= new VolumetricLightingPass();
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
                _frameCount = (_frameCount + 1) % 14;

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
                
                RenderPassEvent volumetricPassEvent = RenderPassEvent.AfterRenderingOpaques;
                bool isRendererDeferred = renderer is UniversalRenderer { renderingModeRequested: RenderingMode.Deferred };
                if (isRendererDeferred) volumetricPassEvent = RenderPassEvent.AfterRenderingGbuffer;
                
                m_GenerateMaxZPass.Setup(volumetricPassEvent, isRendererDeferred, vBufferParams, ref m_MaxZMask);
                
                descriptor.width = VolumetricLightingUtils.s_CurrentVolumetricBufferSize.x;
                descriptor.height = VolumetricLightingUtils.s_CurrentVolumetricBufferSize.y;
                descriptor.volumeDepth = VolumetricLightingUtils.s_CurrentVolumetricBufferSize.z;
                descriptor.dimension = TextureDimension.Tex3D;
                descriptor.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
                descriptor.enableRandomWrite = true;
                RenderingUtils.ReAllocateIfNeeded(ref m_VolumetricDensityBuffer, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "Volumetric Density");
                
                m_ClearAndHeightFogVoxelizationPass.Setup(volumetricPassEvent, vBufferParams, ref m_VolumetricDensityBuffer);
            }
            
            m_OpaqueAtmosphereScatteringPass.Setup(m_ShaderVariablesEnvironment);
        }

        protected override void Dispose(bool disposing)
        {
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
            _frameCount = 0;
            vBufferParams = null;
            m_MaxZMask?.Release();
        }
    }
    
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
    
    internal static class EnvironmentsUtils
    {
        internal static void UpdateShaderVariablesEnvironmentsCB(ref ShaderVariablesEnvironments cb, RenderingData renderingData)
        {
            var universalCamera = renderingData.cameraData;
            bool isMainLightingExists = renderingData.lightData.mainLightIndex >= 0;
            var fogSettings = VolumeManager.instance.stack.GetComponent<Fog>();
            
            fogSettings.UpdateShaderVariablesEnvironmentsCBFogParameters(ref cb, universalCamera, isMainLightingExists);
        }
    }
}
