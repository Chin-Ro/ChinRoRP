//--------------------------------------------------------------------------------------------------------
//  Copyright (C) 2025
//  Author: Chin.Ro
//  体素化Buffer Pass
//--------------------------------------------------------------------------------------------------------

namespace UnityEngine.Rendering.Universal
{
    public class ClearAndHeightFogVoxelizationPass : ScriptableRenderPass
    {
        class HeightFogVoxelizationPassData
        {
            public ComputeShader voxelizationCS;
            public int voxelizationKernel;

            public Vector4 resolution;
            public int viewCount;

            public ShaderVariablesVolumetric volumetricCB;

            public RTHandle densityBuffer;
            public ComputeBuffer volumetricAmbientProbeBuffer;
        }

        private HeightFogVoxelizationPassData passData;
        private VBufferParameters[] vBufferParams;
        
        public ClearAndHeightFogVoxelizationPass(EnvironmentsData data)
        {
            passData = new HeightFogVoxelizationPassData()
            {
                voxelizationCS = data.volumeVoxelizationCS
            };
        }
        
        internal void Setup(ref RTHandle volumetricDensityBuffer, RenderPassEvent passEvent, in VBufferParameters[] m_VBufferParameters, in ShaderVariablesVolumetric m_ShaderVariablesVolumetric)
        {
            renderPassEvent = passEvent;
            passData.densityBuffer = volumetricDensityBuffer;
            vBufferParams = m_VBufferParameters;
            passData.volumetricCB = m_ShaderVariablesVolumetric;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!Fog.IsVolumetricFogEnabled(renderingData.cameraData)) return;
            
            int frameIndex = EnvironmentsRendererFeature.frameIndex;
            var currIdx = (frameIndex + 0) & 1;
            var currParams = vBufferParams[currIdx];

            passData.viewCount = 1;
            
            passData.voxelizationKernel = 0;

            var cvp = currParams.viewportSize;
            passData.resolution = new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y);
            
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, new ProfilingSampler("Clear And Height Fog Voxelization")))
            {
                var data = passData;
                cmd.SetComputeTextureParam(data.voxelizationCS, data.voxelizationKernel, EnvironmentConstants._VBufferDensity, data.densityBuffer);
                
                ConstantBuffer.Push(cmd, data.volumetricCB, data.voxelizationCS, EnvironmentConstants._ShaderVariablesVolumetric);
                cmd.DispatchCompute(data.voxelizationCS, data.voxelizationKernel, ((int)data.resolution.x + 7) / 8, ((int)data.resolution.y + 7) / 8, data.viewCount);
            }
        }
        
        public void Dispose()
        {
            passData = null;
        }
    }
}