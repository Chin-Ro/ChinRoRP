//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  体素光照计算Pass
//--------------------------------------------------------------------------------------------------------

namespace UnityEngine.Rendering.Universal
{
    public class VolumetricLightingPass : ScriptableRenderPass
    {
        class VolumetricLightingPassData
        {
            public ComputeShader volumetricLightingCS;
            public ComputeShader volumetricLightingFilteringCS;
            public int volumetricLightingKernel;
            public int volumetricFilteringKernel;
            public Vector4 resolution;
            public bool enableReprojection;
            public int viewCount;
            public int sliceCount;
            public bool filterVolume;
            public ShaderVariablesVolumetric volumetricCB;

            public RTHandle densityBuffer;
            public RTHandle lightingBuffer;
            public RTHandle maxZBuffer;
            public RTHandle historyBuffer;
            public RTHandle feedbackBuffer;
        }

        private VolumetricLightingPassData passData;
        private VBufferParameters[] vBufferParams;
        
        public VolumetricLightingPass(EnvironmentsData data)
        {
            passData = new VolumetricLightingPassData()
            {
                volumetricLightingCS = data.volumetricLightingCS,
                volumetricLightingFilteringCS = data.volumetricLightingFilteringCS
            };
        }

        internal void Setup(ref RTHandle volumetricLightingBuffer, in RTHandle densityBuffer, in RTHandle maxZBuffer, RenderPassEvent passEvent,
            VBufferParameters[] m_VBufferParameters, ShaderVariablesVolumetric m_ShaderVariablesVolumetricCB)
        {
            renderPassEvent = passEvent;
            passData.lightingBuffer = volumetricLightingBuffer;
            vBufferParams = m_VBufferParameters;
            passData.volumetricCB = m_ShaderVariablesVolumetricCB;
            passData.densityBuffer = densityBuffer;
            passData.maxZBuffer = maxZBuffer;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!Fog.IsVolumetricFogEnabled(renderingData.cameraData)) return;
            
            int frameIndex = EnvironmentsRendererFeature.frameIndex;
            var currIdx = (frameIndex + 0) & 1;
            var prevIdx = (frameIndex + 1) & 1;

            var currParams = vBufferParams[currIdx];

            // Get the interpolated anisotropy value.
            var fog = VolumeManager.instance.stack.GetComponent<Fog>();
            
            // Only available in the Play Mode because all the frame counters in the Edit Mode are broken.
            passData.enableReprojection = Fog.IsVolumetricReprojectionEnabled(renderingData.cameraData);
            bool enableAnisotropy = fog.anisotropy.value != 0;
            passData.volumetricLightingCS.shaderKeywords = null;
            passData.volumetricLightingFilteringCS.shaderKeywords = null;
            
            CoreUtils.SetKeyword(passData.volumetricLightingCS, "ENABLE_REPROJECTION", passData.enableReprojection);
            CoreUtils.SetKeyword(passData.volumetricLightingCS, "ENABLE_ANISOTROPY", enableAnisotropy);
            CoreUtils.SetKeyword(passData.volumetricLightingCS, "SUPPORT_LOCAL_LIGHTS", !fog.directionalLightsOnly.value);
            
            passData.volumetricLightingKernel = passData.volumetricLightingCS.FindKernel("VolumetricLighting");

            passData.volumetricFilteringKernel = passData.volumetricLightingFilteringCS.FindKernel("FilterVolumetricLighting");
            
            var cvp = currParams.viewportSize;
            
            passData.resolution = new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y);
            passData.viewCount = 1;
            passData.filterVolume = ((int)EnvironmentsRendererFeature.m_DenoisingMode & (int)FogDenoisingMode.Gaussian) != 0;
            passData.sliceCount = (int)(cvp.z);

            if (passData.enableReprojection)
            {
                passData.feedbackBuffer = renderingData.cameraData.volumetricHistoryBuffers[currIdx];
                passData.historyBuffer = renderingData.cameraData.volumetricHistoryBuffers[prevIdx];
            }
            
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, new ProfilingSampler("Volumetric Lighting")))
            {
                var data = passData;
                cmd.SetComputeTextureParam(data.volumetricLightingCS, data.volumetricLightingKernel, EnvironmentConstants._MaxZMaskTexture, data.maxZBuffer);  // Read
                cmd.SetComputeTextureParam(data.volumetricLightingCS, data.volumetricLightingKernel, EnvironmentConstants._VBufferDensity, data.densityBuffer);  // Read
                cmd.SetComputeTextureParam(data.volumetricLightingCS, data.volumetricLightingKernel, EnvironmentConstants._VBufferLighting, data.lightingBuffer); // Write

                if (data.enableReprojection)
                {
                    cmd.SetComputeTextureParam(data.volumetricLightingCS, data.volumetricLightingKernel, EnvironmentConstants._VBufferHistory, data.historyBuffer);  // Read
                    cmd.SetComputeTextureParam(data.volumetricLightingCS, data.volumetricLightingKernel, EnvironmentConstants._VBufferFeedback, data.feedbackBuffer); // Write
                }
                ConstantBuffer.Push(cmd, data.volumetricCB, data.volumetricLightingCS, EnvironmentConstants._ShaderVariablesVolumetric);
                
                // The shader defines GROUP_SIZE_1D = 8.
                cmd.DispatchCompute(data.volumetricLightingCS, data.volumetricLightingKernel, ((int)data.resolution.x + 7) / 8, ((int)data.resolution.y + 7) / 8, data.viewCount);

                if (data.filterVolume)
                {
                    ConstantBuffer.Push(cmd, data.volumetricCB, data.volumetricLightingFilteringCS, EnvironmentConstants._ShaderVariablesVolumetric);
                    
                    // The shader defines GROUP_SIZE_1D_XY = 8 and GROUP_SIZE_1D_Z = 1
                    cmd.SetComputeTextureParam(data.volumetricLightingFilteringCS, data.volumetricFilteringKernel, EnvironmentConstants._VBufferLighting, data.lightingBuffer);

                    cmd.DispatchCompute(data.volumetricLightingFilteringCS, data.volumetricFilteringKernel, UniversalUtils.DivRoundUp((int)data.resolution.x, 8),
                        UniversalUtils.DivRoundUp((int)data.resolution.y, 8),
                        data.sliceCount);
                }
                
                cmd.SetGlobalTexture(EnvironmentConstants._VBufferLighting, data.lightingBuffer);
            }

            if (passData.enableReprojection && renderingData.cameraData.volumetricValidFrames > 1)
                renderingData.cameraData.volumetricHistoryIsValid = true; // For the next frame..
            else
                renderingData.cameraData.volumetricValidFrames++;
        }
        
        public void Dispose()
        {
            passData = null;
        }
    }
}