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
            public RTHandle filteringOutputBuffer;
            public RTHandle maxZBuffer;
            public RTHandle historyBuffer;
            public RTHandle feedbackBuffer;
        }

        private VolumetricLightingPassData passData;
        private VBufferParameters[] vBufferParams;

        private RTHandle[] volumetricHistoryBuffers;
        
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
            
            int frameIndex = EnvironmentsRenderFeature.frameIndex;
            var currIdx = (frameIndex + 0) & 1;
            var prevIdx = (frameIndex + 1) & 1;

            var currParams = vBufferParams[currIdx];

            // Get the interpolated anisotropy value.
            var fog = VolumeManager.instance.stack.GetComponent<Fog>();
            
            // Only available in the Play Mode because all the frame counters in the Edit Mode are broken.
            bool volumeAllowsReprojection = ((int)fog.denoisingMode.value & (int)Fog.FogDenoisingMode.Reprojection) != 0;
            passData.enableReprojection = volumeAllowsReprojection;
            bool enableAnisotropy = fog.anisotropy.value != 0;
            // The multi-pass integration is only possible if re-projection is possible and the effect is not in anisotropic mode.
            bool optimal = currParams.voxelSize == 8;
            passData.volumetricLightingCS.shaderKeywords = null;
            passData.volumetricLightingFilteringCS.shaderKeywords = null;
            
            CoreUtils.SetKeyword(passData.volumetricLightingCS, "LIGHTLOOP_DISABLE_TILE_AND_CLUSTER", true);
            CoreUtils.SetKeyword(passData.volumetricLightingCS, "ENABLE_REPROJECTION", passData.enableReprojection);
            CoreUtils.SetKeyword(passData.volumetricLightingCS, "ENABLE_ANISOTROPY", enableAnisotropy);
            CoreUtils.SetKeyword(passData.volumetricLightingCS, "VL_PRESET_OPTIMAL", optimal);
            CoreUtils.SetKeyword(passData.volumetricLightingCS, "SUPPORT_LOCAL_LIGHTS", !fog.directionalLightsOnly.value);
            
            passData.volumetricLightingKernel = passData.volumetricLightingCS.FindKernel("VolumetricLighting");

            passData.volumetricFilteringKernel = passData.volumetricLightingFilteringCS.FindKernel("FilterVolumetricLighting");
            
            var cvp = currParams.viewportSize;
            
            passData.resolution = new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y);
            passData.viewCount = 1;
            passData.filterVolume = ((int)fog.denoisingMode.value & (int)Fog.FogDenoisingMode.Gaussian) != 0;
            passData.sliceCount = (int)(cvp.z);
            
            
        }
        
        public void Dispose()
        {
            
        }
    }
}